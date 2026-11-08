using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Favi_BE.Common;
using Favi_BE.Data;
using Favi_BE.API.Models.Entities;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;
using Favi_BE.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

internal static class ExperimentalFeedChecks
{
    public static async Task RunAsync(Uri address, JsonElement snapshot, string? artifactPath,
        Func<AppDbContext> database, Action<bool,string> check, string logs)
    {
        var records = new List<object>();
        var clients = new List<HttpClient>();
        bool passed = false;
        var methods = new[] { "ease", "ppr" };
        var jwt = new JwtService(Options.Create(new JwtOptions
        {
            Key = "local-oasis-test-only-signing-key-123456789012345678901234567890123456",
            Issuer = "oasis-smoke", Audience = "oasis-smoke", AccessMinutes = 20
        }));
        try
        {
            var anonymous = Client(null);
            await Request(anonymous, "/api/discovery/experiments/feed", HttpStatusCode.Unauthorized, "anonymous");
            if (artifactPath is null)
            {
                var user = snapshot.GetProperty("users")[0].GetProperty("id").GetGuid();
                await Request(Client(user), "/api/discovery/experiments/feed", HttpStatusCode.NotFound, "disabled-default");
                passed = true;
                return;
            }
            var artifact = JsonDocument.Parse(await File.ReadAllTextAsync(artifactPath)).RootElement.Clone();
            var likes = snapshot.GetProperty("likes").EnumerateArray().ToArray();
            var userRows = artifact.GetProperty("users").EnumerateArray().ToArray();
            var row = userRows.First(u => likes.Any(l => l.GetProperty("user_id").GetGuid()==u.GetProperty("userId").GetGuid()) &&
                u.GetProperty("ease").GetArrayLength() >= 15);
            var viewer = row.GetProperty("userId").GetGuid();
            var client = Client(viewer);
            if (DateTimeOffset.UtcNow - artifact.GetProperty("createdAtUtc").GetDateTimeOffset() > TimeSpan.FromHours(24))
            {
                foreach (var method in methods)
                {
                    var stale = await Feed(client, method, "stale-model");
                    check(stale.GetProperty("source").GetString()=="stale-model-fallback", "stale model uses explicit fallback");
                    Safe(stale);
                }
                passed = true;
                return;
            }
            foreach (var query in new[] { "?method=unknown", "?page=0", "?pageSize=101", "?page=10001" })
                await Request(client, "/api/discovery/experiments/feed"+query, HttpStatusCode.BadRequest, "invalid-input");
            var seen = likes.Where(l => l.GetProperty("user_id").GetGuid()==viewer).Select(l => l.GetProperty("post_id").GetGuid()).ToHashSet();
            var expected = methods.ToDictionary(m => m, m => row.GetProperty(m).EnumerateArray()
                .Select(p=>p.GetProperty("postId").GetGuid()).Where(id=>!seen.Contains(id)).ToArray());
            foreach (var method in methods)
            {
                var page1 = await Feed(client, method, "model-page-1", 1, 5);
                var page2 = await Feed(client, method, "model-page-2", 2, 5);
                check(page1.GetProperty("source").GetString()==method, "real "+method+" artifact is served");
                check(page1.GetProperty("snapshotVersion").GetString()==artifact.GetProperty("version").GetString(), "model version survives HTTP");
                check(Ids(page1).Concat(Ids(page2)).SequenceEqual(expected[method].Take(10)), "HTTP preserves real "+method+" score order and GUID mapping across pages");
                check(page1.GetProperty("totalCount").GetInt32()==expected[method].Length, "model total reflects current eligible pool");
                var repeated = await Feed(client, method, "model-repeat", 1, 5);
                check(Ids(repeated).SequenceEqual(Ids(page1)), "unchanged model paging is deterministic");
                var beyond = await Feed(client, method, "empty-page", 10000, 5);
                check(Ids(beyond).Length==0 && !beyond.GetProperty("hasNext").GetBoolean(), "beyond-last page is bounded and empty");
                Safe(page1);
                Safe(page2);
                for (var sample = 0; sample < 20; sample++)
                {
                    var measured = await Feed(client, method, "model-latency-sample", 1, 10);
                    check(measured.GetProperty("source").GetString() == method,
                        "latency sample uses real " + method + " ranks without fallback");
                }
            }

            var target = expected["ease"][0];
            await using (var db = database())
            {
                db.Reactions.Add(new Reaction { Id=Guid.NewGuid(), ProfileId=viewer, PostId=target, Type=ReactionType.Like, CreatedAt=DateTime.UtcNow });
                await db.SaveChangesAsync();
            }
            await Excludes(target, "new-positive-after-artifact");
            var savedTarget = expected["ease"][1];
            await using (var db = database())
            {
                var collection = new Collection { Id=Guid.NewGuid(),ProfileId=viewer,Title="experiment save",CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow };
                db.Collections.Add(collection);
                db.PostCollections.Add(new PostCollection { CollectionId=collection.Id,PostId=savedTarget,Collection=collection });
                await db.SaveChangesAsync();
            }
            await Excludes(savedTarget, "new-save-after-artifact");

            var safetyTarget = expected["ease"][2];
            foreach (var scenario in new[] { "nsfw", "post-private", "archive", "soft-delete", "own-post" })
            {
                Guid originalAuthor;
                await using (var db = database())
                {
                    var post = await db.Posts.SingleAsync(p=>p.Id==safetyTarget);
                    originalAuthor=post.ProfileId;
                    if (scenario=="nsfw") post.IsNSFW=true;
                    if (scenario=="post-private") post.Privacy=PrivacyLevel.Private;
                    if (scenario=="archive") post.IsArchived=true;
                    if (scenario=="soft-delete") post.DeletedDayExpiredAt=DateTime.UtcNow.AddDays(1);
                    if (scenario=="own-post") post.ProfileId=viewer;
                    await db.SaveChangesAsync();
                }
                await Excludes(safetyTarget, scenario);
                await using (var db = database())
                {
                    var post=await db.Posts.SingleAsync(p=>p.Id==safetyTarget);
                    post.IsNSFW=false; post.Privacy=PrivacyLevel.Public; post.IsArchived=false;
                    post.DeletedDayExpiredAt=null; post.ProfileId=originalAuthor;
                    await db.SaveChangesAsync();
                }
            }
            Guid author;
            await using (var db=database()) author=await db.Posts.Where(p=>p.Id==safetyTarget).Select(p=>p.ProfileId).SingleAsync();
            foreach (var scenario in new[] { "profile-private", "author-banned" })
            {
                await using (var db=database())
                {
                    var profile=await db.Profiles.SingleAsync(p=>p.Id==author);
                    if (scenario=="profile-private") profile.PrivacyLevel=PrivacyLevel.Private;
                    else { profile.IsBanned=true; profile.BannedUntil=DateTime.UtcNow.AddDays(1); }
                    await db.SaveChangesAsync();
                }
                foreach (var method in methods)
                {
                    var response=await Feed(client,method,scenario);
                    check(response.GetProperty("data").EnumerateArray().All(p=>p.GetProperty("authorProfileId").GetGuid()!=author), scenario+" is filtered after model ranking");
                }
                await using (var db=database())
                {
                    var profile=await db.Profiles.SingleAsync(p=>p.Id==author);
                    profile.PrivacyLevel=PrivacyLevel.Public; profile.IsBanned=false; profile.BannedUntil=null;
                    await db.SaveChangesAsync();
                }
            }

            var coldId=Guid.NewGuid();
            await using (var db=database())
            {
                db.Profiles.Add(new Profile { Id=coldId,Username="experiment_cold_"+coldId.ToString("N"),CreatedAt=DateTime.UtcNow });
                await db.SaveChangesAsync();
            }
            var coldClient=Client(coldId);
            foreach (var method in methods)
            {
                var response=await Feed(coldClient,method,"cold-fallback");
                check(response.GetProperty("source").GetString()=="cold-fallback" && Ids(response).Length>0, "cold user uses real topic/catalog fallback");
                Safe(response);
            }
            var newLike=Guid.NewGuid();
            await using (var db=database())
            {
                db.Reactions.Add(new Reaction { Id=newLike,ProfileId=coldId,PostId=safetyTarget,Type=ReactionType.Love,CreatedAt=DateTime.UtcNow });
                await db.SaveChangesAsync();
            }
            foreach (var method in methods)
            {
                var response=await Feed(coldClient,method,"missing-model-warm");
                check(response.GetProperty("source").GetString()=="missing-model-fallback", "new warm viewer without embedding/rank row uses fallback");
                check(!Ids(response).Contains(safetyTarget), "fallback excludes new viewer positive");
                Safe(response);
            }
            await using (var db=database())
            {
                db.Reactions.Remove(await db.Reactions.SingleAsync(r=>r.Id==newLike));
                await db.SaveChangesAsync();
            }
            check((await Feed(coldClient,"ease","last-positive-removed")).GetProperty("source").GetString()=="cold-fallback", "removing last positive returns to cold fallback");

            var modelIds=row.GetProperty("ease").EnumerateArray().Concat(row.GetProperty("ppr").EnumerateArray()).Select(p=>p.GetProperty("postId").GetGuid()).Distinct().ToArray();
            await using (var db=database()) await db.Posts.Where(p=>modelIds.Contains(p.Id)).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.IsNSFW,true));
            foreach (var method in methods)
            {
                var response=await Feed(client,method,"all-model-candidates-unsafe");
                check(response.GetProperty("source").GetString()=="empty-model-fallback" && Ids(response).Length==0, "empty safe model pool falls back without relaxing NSFW or seen exclusion");
            }
            await using (var db=database()) await db.Posts.Where(p=>modelIds.Contains(p.Id)).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.IsNSFW,false));
            await using (var db=database())
            {
                var profile=await db.Profiles.SingleAsync(p=>p.Id==viewer);
                profile.IsBanned=true; profile.BannedUntil=DateTime.UtcNow.AddDays(1);
                await db.SaveChangesAsync();
            }
            check(Ids(await Feed(client,"ease","viewer-banned")).Length==0, "banned viewer receives no experimental content");
            passed=true;

            async Task Excludes(Guid postId,string scenario)
            {
                foreach(var method in methods)
                {
                    var response=await Feed(client,method,scenario);
                    check(!Ids(response).Contains(postId),scenario+" excluded by "+method);
                    Safe(response);
                }
            }
        }
        finally
        {
            foreach(var client in clients) client.Dispose();
            await File.WriteAllTextAsync(Path.Combine(logs,"experimental-feed.json"),JsonSerializer.Serialize(new { passed,artifactPath,requests=records },new JsonSerializerOptions { WriteIndented=true }));
        }

        HttpClient Client(Guid? user)
        {
            var client=new HttpClient { BaseAddress=address,Timeout=TimeSpan.FromSeconds(30) };
            if(user.HasValue) client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",jwt.CreateAccessToken(user.Value,"experiment-viewer","User"));
            clients.Add(client);
            return client;
        }
        Task<JsonElement> Feed(HttpClient client,string method,string scenario,int page=1,int size=100)
            =>Request(client,$"/api/discovery/experiments/feed?method={method}&page={page}&pageSize={size}",HttpStatusCode.OK,scenario);
        async Task<JsonElement> Request(HttpClient client,string path,HttpStatusCode expected,string scenario)
        {
            var timer=Stopwatch.StartNew();
            using var response=await client.GetAsync(path);
            var elapsed=timer.Elapsed.TotalMilliseconds;
            check(response.StatusCode==expected,$"experiment {scenario}: {(int)response.StatusCode} expected {(int)expected}");
            JsonElement? body=response.StatusCode==HttpStatusCode.OK ? (await response.Content.ReadFromJsonAsync<JsonElement>()).Clone() : null;
            records.Add(new { scenario,path,status=(int)response.StatusCode,latencyMs=elapsed,response=body });
            return body ?? default;
        }
        static Guid[] Ids(JsonElement response)=>response.GetProperty("data").EnumerateArray().Select(p=>p.GetProperty("id").GetGuid()).ToArray();
        void Safe(JsonElement response)
        {
            var rows=response.GetProperty("data").EnumerateArray().ToArray();
            check(rows.All(p=>!p.GetProperty("isNSFW").GetBoolean()) && Ids(response).Distinct().Count()==rows.Length,"experimental page is unique and NSFW-safe");
            check(response.GetProperty("authorDiversity").GetDouble() is >=0 and <=1 && response.GetProperty("maxAuthorShare").GetDouble() is >=0 and <=1,"author diagnostics stay bounded");
            if(rows.Length>0) check(response.GetProperty("meanAgeHours").GetDouble()>=0,"freshness diagnostic is nonnegative real UTC age");
        }
    }
}
