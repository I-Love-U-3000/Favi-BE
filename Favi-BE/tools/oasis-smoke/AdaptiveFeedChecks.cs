using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Favi_BE.API.Models.Entities;
using Favi_BE.Common;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;
using Favi_BE.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// Schema 2 integration checks use the original GUIDs and scores, with controlled
// live state in this runner's disposable database. No model or source is edited.
internal static class AdaptiveFeedChecks
{
    public static async Task RunAsync(Uri address, JsonElement artifact,
        Func<AppDbContext> database, Action<bool,string> check, string logs)
    {
        var records = new List<object>();
        var passed = false;
        var jwt = new JwtService(Options.Create(new JwtOptions {
            Key="local-oasis-test-only-signing-key-123456789012345678901234567890123456",
            Issuer="oasis-smoke", Audience="oasis-smoke", AccessMinutes=120 }));
        using var client = new HttpClient { BaseAddress=address, Timeout=TimeSpan.FromSeconds(60) };
        var methods = new[] { "B1", "R" };
        try
        {
            await Send(HttpMethod.Get,"/api/discovery/experiments/feed",HttpStatusCode.Unauthorized);
            var row = artifact.GetProperty("users").EnumerateArray().First(u =>
                methods.All(m => u.GetProperty("ranks").GetProperty(m).GetArrayLength() >= 20));
            var viewer = row.GetProperty("userId").GetGuid();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",jwt.CreateAccessToken(viewer,"adaptive-smoke","User"));
            var ranks = methods.ToDictionary(m=>m,m=>row.GetProperty("ranks").GetProperty(m).EnumerateArray().Select(p=>p.GetProperty("postId").GetGuid()).ToArray());
            await using (var db=database())
            {
                check(await db.Profiles.AnyAsync(p=>p.Id==viewer),"artifact viewer GUID exists in imported dataset");
                var ids=ranks.Values.SelectMany(x=>x).Distinct().ToArray();
                check(await db.Posts.CountAsync(p=>ids.Contains(p.Id))==ids.Length,"every selected rank GUID exists in PostgreSQL");
                await db.Reactions.Where(r=>r.ProfileId==viewer).ExecuteDeleteAsync();
                var collections=await db.Collections.Where(c=>c.ProfileId==viewer).Select(c=>c.Id).ToArrayAsync();
                await db.PostCollections.Where(p=>collections.Contains(p.CollectionId)).ExecuteDeleteAsync();
            }
            foreach(var query in new[]{"?method=unknown","?page=0","?pageSize=101","?page=10001"})
                await Send(HttpMethod.Get,"/api/discovery/experiments/feed"+query,HttpStatusCode.BadRequest);
            foreach(var method in methods) check(Source(await Feed(method))=="cold-fallback","zero positives route cold: "+method);
            var stale=DateTimeOffset.UtcNow-artifact.GetProperty("createdAtUtc").GetDateTimeOffset()>TimeSpan.FromHours(24);
            Guid[] positives;
            await using(var db=database())
                positives=await db.Posts.Where(p=>p.ProfileId!=viewer).OrderBy(p=>p.Id).Select(p=>p.Id).Take(5).ToArrayAsync();
            for(var count=1;count<=5;count++)
            {
                await using(var db=database())
                {
                    db.Reactions.Add(new Reaction { Id=Guid.NewGuid(),ProfileId=viewer,PostId=positives[count-1],Type=ReactionType.Like,CreatedAt=DateTime.UtcNow });
                    if(count==4)
                    {
                        var collection=new Collection { Id=Guid.NewGuid(),ProfileId=viewer,Title="dedup",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow };
                        db.Collections.Add(collection);
                        db.PostCollections.Add(new PostCollection { CollectionId=collection.Id,Collection=collection,PostId=positives[0] });
                    }
                    await db.SaveChangesAsync();
                }
                foreach(var method in methods)
                {
                    var threshold=artifact.GetProperty("methods").GetProperty(method).GetProperty("threshold").GetInt32();
                    var response=await Feed(method);
                    check(Source(response)==(count<threshold?"cold-fallback":stale?"stale-model-fallback":method),$"{method} routes at {count} distinct positives (duplicate save does not advance threshold)");
                    check(!Ids(response).Intersect(positives.Take(count)).Any(),"current positives excluded");
                }
            }
            if(stale) { passed=true; return; }
            foreach(var method in methods)
            {
                Guid[] expected;
                await using(var db=database())
                {
                    var ids=ranks[method];
                    var eligible=await db.Posts.Where(p=>ids.Contains(p.Id)&&p.ProfileId!=viewer&&!p.IsNSFW&&!p.IsArchived&&p.DeletedDayExpiredAt==null&&p.Privacy==PrivacyLevel.Public&&p.Profile.PrivacyLevel==PrivacyLevel.Public&&!p.Profile.IsBanned).Select(p=>p.Id).ToArrayAsync();
                    expected=ids.Where(id=>eligible.Contains(id)&&!positives.Contains(id)).ToArray();
                }
                var first=await Feed(method,1,5); var second=await Feed(method,2,5);
                check(Ids(first).Concat(Ids(second)).SequenceEqual(expected.Take(10)),"original scores/GUID order across pages: "+method);
                check(first.GetProperty("totalCount").GetInt32()==expected.Length,"eligible total before pagination");
                check(Ids(await Feed(method,1,5)).SequenceEqual(Ids(first)),"repeat paging stable");
                check(Ids(await Feed(method,10000,5)).Length==0,"beyond-last page empty");
                check(first.GetProperty("snapshotVersion").GetString()==artifact.GetProperty("version").GetString(),"HTTP preserves artifact version");
            }
            foreach(var method in methods)
            {
                async Task<(double Ms,bool Valid)> Measure()
                {
                    var timer=Stopwatch.StartNew();
                    using var response=await client.GetAsync($"/api/discovery/experiments/feed?method={method}&pageSize=10");
                    var payload=await response.Content.ReadFromJsonAsync<JsonElement>();
                    return (timer.Elapsed.TotalMilliseconds,response.IsSuccessStatusCode&&Source(payload)==method&&
                        !Ids(payload).Intersect(positives).Any()&&payload.GetProperty("data").EnumerateArray().All(p=>!p.GetProperty("isNSFW").GetBoolean()));
                }
                async Task<(double Ms,bool Valid)[]> Batch(int count)
                {
                    var output=new (double Ms,bool Valid)[count];
                    await Parallel.ForEachAsync(Enumerable.Range(0,count),new ParallelOptions{MaxDegreeOfParallelism=10},async(i,ct)=>output[i]=await Measure());
                    return output;
                }
                var warmup=await Batch(100);var samples=await Batch(1000);
                var ordered=samples.Select(s=>s.Ms).Order().ToArray();var p95=ordered[(int)Math.Ceiling(.95*ordered.Length)-1];
                var valid=warmup.Concat(samples).All(s=>s.Valid);
                await File.WriteAllTextAsync(Path.Combine(logs,$"adaptive-load-{method}.json"),JsonSerializer.Serialize(new{transport="direct",method,concurrency=10,warmup=100,measured=1000,p95Ms=p95,valid,latencyGatePassed=p95<=100,samples},new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));
                check(valid,"concurrent load has no status/source/positive/NSFW violations: "+method);
                // Preserve a failed latency result without aborting later privacy checks.
                records.Add(new{scenario="frozen-direct-load",method,p95Ms=p95,latencyGatePassed=p95<=100});
            }
            await AdaptiveGatewayProbe.RunAsync(address,client.DefaultRequestHeaders.Authorization!,logs);
            var safety=Ids(await Feed("R")).First();
            foreach(var scenario in new[]{"nsfw","private","archive","delete","own","author-private","author-ban"})
            {
                Guid author;
                await using(var db=database())
                {
                    var post=await db.Posts.Include(p=>p.Profile).SingleAsync(p=>p.Id==safety); author=post.ProfileId;
                    if(scenario=="nsfw") post.IsNSFW=true;
                    if(scenario=="private") post.Privacy=PrivacyLevel.Private;
                    if(scenario=="archive") post.IsArchived=true;
                    if(scenario=="delete") post.DeletedDayExpiredAt=DateTime.UtcNow.AddDays(1);
                    if(scenario=="own") post.ProfileId=viewer;
                    if(scenario=="author-private") post.Profile.PrivacyLevel=PrivacyLevel.Private;
                    if(scenario=="author-ban") { post.Profile.IsBanned=true; post.Profile.BannedUntil=DateTime.UtcNow.AddDays(1); }
                    await db.SaveChangesAsync();
                }
                foreach(var method in methods) check(!Ids(await Feed(method)).Contains(safety),scenario+" excluded: "+method);
                await using(var db=database())
                {
                    var post=await db.Posts.SingleAsync(p=>p.Id==safety);
                    post.IsNSFW=false;post.Privacy=PrivacyLevel.Public;post.IsArchived=false;post.DeletedDayExpiredAt=null;post.ProfileId=author;
                    var profile=await db.Profiles.SingleAsync(p=>p.Id==author);profile.PrivacyLevel=PrivacyLevel.Public;profile.IsBanned=false;profile.BannedUntil=null;
                    await db.SaveChangesAsync();
                }
            }
            // The fresh account is deliberately absent from the learned artifact.
            var newViewer=Guid.NewGuid();
            var foodPost=Guid.NewGuid();var artPost=Guid.NewGuid();
            await using(var db=database())
            {
                db.Profiles.Add(new Profile {Id=newViewer,Username="adaptive_"+newViewer.ToString("N"),CreatedAt=DateTime.UtcNow});
                var author=Guid.NewGuid();db.Profiles.Add(new Profile{Id=author,Username="adaptive_author_"+author.ToString("N"),CreatedAt=DateTime.UtcNow});
                foreach(var pair in new[]{(Id:foodPost,Topic:"food"),(Id:artPost,Topic:"art")})
                {
                    var tag=await db.Tags.FirstOrDefaultAsync(t=>t.Name==pair.Topic)??new Tag{Id=Guid.NewGuid(),Name=pair.Topic};
                    db.Posts.Add(new Post{Id=pair.Id,ProfileId=author,Caption="new topic marker",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow,Privacy=PrivacyLevel.Public,
                        PostTags=new List<PostTag>{new(){TagId=tag.Id,Tag=tag}}});
                }
                await db.SaveChangesAsync();
            }
            client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",jwt.CreateAccessToken(newViewer,"new-viewer","User"));
            var initial=await Send(HttpMethod.Get,"/api/profiles/me/interests",HttpStatusCode.OK);
            check(initial.GetProperty("version").GetInt32()==0,"new interests version zero");
            var body=new {topics=new[]{"food"},version=0};var key=Guid.NewGuid().ToString();
            await Send(HttpMethod.Put,"/api/profiles/me/interests",HttpStatusCode.BadRequest,body);
            var updated=await Send(HttpMethod.Put,"/api/profiles/me/interests",HttpStatusCode.OK,body,key);
            var replay=await Send(HttpMethod.Put,"/api/profiles/me/interests",HttpStatusCode.OK,body,key);
            check(updated.GetProperty("version").GetInt32()==1&&replay.GetProperty("version").GetInt32()==1,"idempotent replay updates once");
            await Send(HttpMethod.Put,"/api/profiles/me/interests",HttpStatusCode.Conflict,new{topics=new[]{"art"},version=0},Guid.NewGuid().ToString());
            var persisted=await Send(HttpMethod.Get,"/api/profiles/me/interests",HttpStatusCode.OK);
            check(persisted.GetProperty("topics")[0].GetString()=="food","stale topic update preserves winner");
            check(Source(await Feed("R"))=="cold-fallback","new viewer uses current topics");
            check(Ids(await Feed("R",1,1)).Single()==foodPost,"new item with selected food topic appears immediately");
            await Send(HttpMethod.Put,"/api/profiles/me/interests",HttpStatusCode.OK,new{topics=new[]{"art"},version=1},Guid.NewGuid().ToString());
            check(Ids(await Feed("R",1,1)).Single()==artPost,"current topic change immediately selects new art item");
            await Send(HttpMethod.Put,"/api/profiles/me/interests",HttpStatusCode.OK,new{topics=Array.Empty<string>(),version=2},Guid.NewGuid().ToString());
            check(Source(await Feed("R"))=="cold-fallback","cleared topics retain general fallback");
            await using(var db=database()) { foreach(var id in positives) db.Reactions.Add(new Reaction {Id=Guid.NewGuid(),ProfileId=newViewer,PostId=id,Type=ReactionType.Love,CreatedAt=DateTime.UtcNow});await db.SaveChangesAsync(); }
            foreach(var method in methods) {var response=await Feed(method);check(Source(response)=="missing-model-fallback","new warm viewer explicit missing-model fallback");check(!Ids(response).Intersect(positives).Any(),"missing-model fallback excludes positives");}
            await using(var db=database()) {await db.Reactions.Where(r=>r.ProfileId==newViewer).ExecuteDeleteAsync();}
            check(Source(await Feed("R"))=="cold-fallback","removing last positive returns to cold");
            await using(var db=database())
            {
                var collection=new Collection{Id=Guid.NewGuid(),ProfileId=newViewer,Title="save-only threshold",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};db.Collections.Add(collection);
                foreach(var id in positives) db.PostCollections.Add(new PostCollection{CollectionId=collection.Id,Collection=collection,PostId=id});await db.SaveChangesAsync();
            }
            var savedOnly=await Feed("R");
            check(Source(savedOnly)=="missing-model-fallback","five distinct saves alone reach warm threshold");
            check(!Ids(savedOnly).Intersect(positives).Any(),"save-only fallback excludes every saved item");
            await using(var db=database()) { var p=await db.Profiles.SingleAsync(p=>p.Id==newViewer);p.IsBanned=true;p.BannedUntil=DateTime.UtcNow.AddDays(1);await db.SaveChangesAsync(); }
            foreach(var method in methods) check(Ids(await Feed(method)).Length==0,"banned viewer empty");
            client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",jwt.CreateAccessToken(viewer,"adaptive-smoke","User"));
            var allModelIds=ranks.Values.SelectMany(x=>x).Distinct().ToArray();
            await using(var db=database()) await db.Posts.Where(p=>allModelIds.Contains(p.Id)).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.IsNSFW,true));
            foreach(var method in methods)
            {
                var response=await Feed(method);check(Source(response)=="empty-model-fallback","all unsafe saved ranks use explicit empty-model fallback");
                check(!Ids(response).Intersect(allModelIds).Any(),"fallback never relaxes unsafe saved rank exclusion");
            }
            check(Source(await Feed("ease"))=="missing-model-fallback","schema2 never aliases legacy ease to B1/R");
            passed=true;
        }
        finally { await File.WriteAllTextAsync(Path.Combine(logs,"adaptive-http.json"),JsonSerializer.Serialize(new{passed,transport="direct",requests=records},new JsonSerializerOptions{WriteIndented=true})); }

        Task<JsonElement> Feed(string method,int page=1,int size=100)=>Send(HttpMethod.Get,$"/api/discovery/experiments/feed?method={method}&page={page}&pageSize={size}",HttpStatusCode.OK);
        async Task<JsonElement> Send(HttpMethod method,string path,HttpStatusCode expected,object? body=null,string? key=null)
        {
            using var request=new HttpRequestMessage(method,path);if(body!=null)request.Content=JsonContent.Create(body);if(key!=null)request.Headers.Add("Idempotency-Key",key);
            var timer=Stopwatch.StartNew();using var response=await client.SendAsync(request);var text=await response.Content.ReadAsStringAsync();
            records.Add(new{method=method.Method,path,status=(int)response.StatusCode,latencyMs=timer.Elapsed.TotalMilliseconds,body=text});
            check(response.StatusCode==expected,$"adaptive {method} {path}: {(int)response.StatusCode} expected {(int)expected}");
            return response.IsSuccessStatusCode?JsonDocument.Parse(text).RootElement.Clone():default;
        }
        static string? Source(JsonElement response)=>response.GetProperty("source").GetString();
        static Guid[] Ids(JsonElement response)=>response.GetProperty("data").EnumerateArray().Select(p=>p.GetProperty("id").GetGuid()).ToArray();
    }
}
