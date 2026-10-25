using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

try { await RunAsync(args); }
catch (Exception error)
{
    Console.Error.WriteLine(error);
    Environment.ExitCode = 1;
}

static async Task RunAsync(string[] args)
{
// Run from the solution directory. Only the dedicated local smoke database is permitted.
var upgrade = args.Contains("--upgrade");
var existingColumns = args.Contains("--existing-columns");
if (existingColumns && !upgrade) throw new ArgumentException("--existing-columns requires --upgrade");
var database = upgrade ? "favi_cold_upgrade_" + Guid.NewGuid().ToString("N") : "favi_cold_smoke";
var connection = $"Host=127.0.0.1;Port=15439;Database={database};Username=favi_smoke;Password=local_smoke_only";
if (upgrade)
{
    await using var admin = new NpgsqlConnection("Host=127.0.0.1;Port=15439;Database=postgres;Username=favi_smoke;Password=local_smoke_only");
    await admin.OpenAsync();
    await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin);
    await create.ExecuteNonQueryAsync();
}
var root = Directory.GetCurrentDirectory();
var apiDir = Path.Combine(root, "Favi-BE.API");
var apiDll = Path.Combine(apiDir, "bin", "Debug", "net9.0", "Favi-BE.API.dll");
if (!File.Exists(apiDll)) throw new InvalidOperationException("Build Favi-BE.API first; run smoke from solution directory.");
var logs = Path.Combine(root, ".tools", upgrade ? (existingColumns ? "cold-start-upgrade-existing" : "cold-start-upgrade") : "cold-start-smoke");
Directory.CreateDirectory(logs);
var checks = new List<string>();
var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options;
await using (var db = new AppDbContext(options))
{
    if (upgrade)
    {
        await db.GetService<IMigrator>().MigrateAsync("20260423000000_AddUpdatedAtAndVersionToProfile");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "Profiles" ("Id", "Username", "DisplayName", "Role", "CreatedAt", "UpdatedAt", "Version", "PrivacyLevel", "FollowPrivacyLevel", "IsBanned")
            VALUES ('10000000-0000-0000-0000-000000000001', 'upgrade_legacy', 'Dữ liệu cũ', 0, '2025-01-01Z', '2025-02-01Z', 7, 0, 0, false);
            INSERT INTO "Posts" ("Id", "ProfileId", "Caption", "Privacy", "CreatedAt", "UpdatedAt", "IsArchived", "IsNSFW")
            VALUES ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'Legacy Unicode: ảnh cũ', 0, '2025-01-02Z', '2025-02-02Z', false, true);
            """);
        if (existingColumns)
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "Posts" ADD COLUMN "Version" integer NOT NULL DEFAULT 1;
                UPDATE "Posts" SET "Version" = 9;
                CREATE INDEX "IX_Posts_Active_CreatedAt" ON "Posts" ("DeletedDayExpiredAt", "IsArchived", "CreatedAt");
                """);
        var beforeProfile = await Snapshot("Profiles", false);
        var beforePosts = await Snapshot("Posts", !existingColumns);
        await db.Database.MigrateAsync();
        Check(beforeProfile == await Snapshot("Profiles", false), "upgrade preserves every legacy profile field including version 7");
        Check(beforePosts == await Snapshot("Posts", !existingColumns), "upgrade preserves every legacy post field");
        var legacyPost = await db.Posts.AsNoTracking().SingleAsync();
        Check(legacyPost.Version == (existingColumns ? 9 : 1) && legacyPost.IsNSFW,
            existingColumns ? "upgrade preserves existing post version 9 and unsafe flag" : "upgrade backfills post version and preserves unsafe flag");
        Check(!await db.Set<Favi_BE.Modules.Auth.Domain.ProfileInterests>().AnyAsync(), "upgrade does not create default interest rows");
        var after = await Snapshot("Posts", false);
        await db.Database.MigrateAsync();
        Check(after == await Snapshot("Posts", false), "second migrate is a no-op for existing data");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "ProfileInterests" ("ProfileId", "Topics", "Version")
            VALUES ('10000000-0000-0000-0000-000000000001', ARRAY['food'], 1);
            """);
        await db.GetService<IMigrator>().MigrateAsync("20260423000000_AddUpdatedAtAndVersionToProfile");
        Check(after == await Snapshot("Posts", false) && beforeProfile == await Snapshot("Profiles", false),
            "rollback preserves legacy rows and compatibility columns");
        await using (var inspect = new NpgsqlConnection(connection))
        {
            await inspect.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT to_regclass('\"ProfileInterests\"') IS NULL", inspect);
            Check((bool)(await command.ExecuteScalarAsync())!, "rollback drops interests table as documented");
        }
        await db.Database.MigrateAsync();
        Check(!await db.Set<Favi_BE.Modules.Auth.Domain.ProfileInterests>().AnyAsync(), "re-upgrade creates empty interests; rollback loses preferences");
        Check(after == await Snapshot("Posts", false), "re-upgrade preserves legacy posts");
    }
    await db.Database.MigrateAsync();
    if (!await db.Posts.AnyAsync(p => p.Caption != null && p.Caption.StartsWith("cold-smoke:")))
    {
        foreach (var topic in new[] { "travel", "food", "art" })
        {
            var author = new Profile { Id = Guid.NewGuid(), Username = "smoke_" + topic,
                CreatedAt = DateTime.UtcNow, DisplayName = topic };
            var tag = new Tag { Id = Guid.NewGuid(), Name = topic };
            db.Profiles.Add(author);
            db.Tags.Add(tag);
            for (var i = 0; i < 5; i++)
            {
                var post = new Post { Id = Guid.NewGuid(), ProfileId = author.Id, Profile = author,
                    Caption = $"cold-smoke:{topic}:{i}", CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                    UpdatedAt = DateTime.UtcNow, IsNSFW = i == 3,
                    Privacy = i == 4 ? PrivacyLevel.Private : PrivacyLevel.Public };
                post.PostTags.Add(new PostTag { PostId = post.Id, TagId = tag.Id, Tag = tag });
                post.PostMedias.Add(new PostMedia { Id = Guid.NewGuid(), PostId = post.Id,
                    ProfileId = author.Id, Url = "/seed-assets/smoke.jpg", PublicId = "smoke",
                    Width = 64, Height = 64, Format = "jpg" });
                db.Posts.Add(post);
            }
        }
        await db.SaveChangesAsync();
    }
}

var start = new ProcessStartInfo("dotnet") { WorkingDirectory = apiDir,
    UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
    CreateNoWindow = true };
start.ArgumentList.Add(apiDll);
foreach (var (key, value) in new Dictionary<string, string>
{
    ["ASPNETCORE_URLS"] = "http://127.0.0.1:18181",
    ["ASPNETCORE_ENVIRONMENT"] = "Production",
    ["ConnectionStrings__Default"] = connection,
    ["Redis__ConnectionString"] = "127.0.0.1:16389,abortConnect=false",
    ["Jwt__Key"] = "local-smoke-jwt-signing-key-only-not-a-deployment-secret-1234567890123456",
    ["Jwt__Issuer"] = "favi-smoke", ["Jwt__Audience"] = "favi-smoke",
    // Controllers construct the media client even on reads; no upload/network call is made.
    ["CloudinarySettings__CloudName"] = "smoke-local", ["CloudinarySettings__ApiKey"] = "smoke",
    ["CloudinarySettings__ApiSecret"] = "smoke",
    ["Seeding__Enabled"] = "false", ["VectorIndex__Enabled"] = "false", ["NSFW__Enabled"] = "false"
}) start.Environment[key] = value;

using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start API");
var stdout = process.StandardOutput.ReadToEndAsync();
var stderr = process.StandardError.ReadToEndAsync();
using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:18181"), Timeout = TimeSpan.FromSeconds(20) };
try
{
    for (var i = 0; ; i++)
    {
        try { if ((await client.GetAsync("/health/live")).IsSuccessStatusCode) break; }
        catch (HttpRequestException) { }
        if (i >= 60 || process.HasExited) throw new InvalidOperationException("API did not become ready; see API logs.");
        await Task.Delay(500);
    }
    await Expect("GET", "/api/profiles/me/interests", null, HttpStatusCode.Unauthorized);
    var topics = await Expect("GET", "/api/discovery/topics", null, HttpStatusCode.OK);
    Check(topics.GetArrayLength() == 10, "topic catalog has 10 entries");
    var suffix = Guid.NewGuid().ToString("N");
    var registered = await Expect("POST", "/api/auth/register", new {
        email = $"smoke_{suffix}@example.test", password = "LocalSmoke123!", username = "smoke_" + suffix,
        displayName = "Cold smoke user" }, HttpStatusCode.OK);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.GetProperty("accessToken").GetString());
    var initial = await Expect("GET", "/api/profiles/me/interests", null, HttpStatusCode.OK);
    Check(initial.GetProperty("version").GetInt32() == 0, "new account interests version=0");
    var general = await Expect("GET", "/api/posts/feed?pageSize=9", null, HttpStatusCode.OK);
    var generalPosts = general.GetProperty("data").EnumerateArray().ToArray();
    Check(generalPosts.Length >= 3, "general feed has real posts");
    Check(generalPosts.Take(3).Select(Topic).Distinct().Count() == 3, "first three general posts span three topics");
    Check(generalPosts.All(p => !p.GetProperty("isNSFW").GetBoolean()), "unsafe posts excluded");
    Check(generalPosts.All(p => p.GetProperty("privacyLevel").GetString() == "Public"), "private posts excluded");
    await Expect("PUT", "/api/profiles/me/interests", new { topics = new[] { "food" }, version = 0 }, HttpStatusCode.BadRequest);
    var key = Guid.NewGuid().ToString();
    var saved = await Expect("PUT", "/api/profiles/me/interests", new { topics = new[] { "food" }, version = 0 }, HttpStatusCode.OK, key);
    Check(saved.GetProperty("version").GetInt32() == 1, "preferences persisted with version 1");
    var replay = await Expect("PUT", "/api/profiles/me/interests", new { topics = new[] { "food" }, version = 0 }, HttpStatusCode.OK, key);
    Check(replay.GetProperty("version").GetInt32() == 1, "Redis replay does not update twice");
    await Expect("PUT", "/api/profiles/me/interests", new { topics = new[] { "art" }, version = 0 }, HttpStatusCode.Conflict, Guid.NewGuid().ToString());
    var persisted = await Expect("GET", "/api/profiles/me/interests", null, HttpStatusCode.OK);
    Check(persisted.GetProperty("topics")[0].GetString() == "food", "stale update leaves food preference intact");
    var selected = await Expect("GET", "/api/posts/feed?pageSize=3", null, HttpStatusCode.OK);
    Check(selected.GetProperty("data").EnumerateArray().All(p => Topic(p) == "food"), "selected-topic feed starts with food");
    var withReposts = await Expect("GET", "/api/posts/feed-with-reposts?pageSize=3", null, HttpStatusCode.OK);
    Check(withReposts.GetProperty("data").EnumerateArray().All(p => Topic(p.GetProperty("post")) == "food"), "feed-with-reposts preserves cold ranking");
    await Expect("GET", "/api/posts/feed?page=0", null, HttpStatusCode.BadRequest);
    await Expect("GET", "/api/posts/feed?pageSize=101", null, HttpStatusCode.BadRequest);
    var page2 = await Expect("GET", "/api/posts/feed?page=2&pageSize=3", null, HttpStatusCode.OK);
    var firstIds = selected.GetProperty("data").EnumerateArray().Select(p => p.GetProperty("id").GetGuid()).ToHashSet();
    Check(page2.GetProperty("data").EnumerateArray().All(p => !firstIds.Contains(p.GetProperty("id").GetGuid())), "stable pages have no duplicate post");
    await Expect("PUT", "/api/profiles/me/interests", new { topics = new[] { "invalid" }, version = 1 }, HttpStatusCode.BadRequest, Guid.NewGuid().ToString());
    await Expect("PUT", "/api/profiles/me/interests", new { topics = new[] { "food", "travel" }, version = 1 }, HttpStatusCode.OK, Guid.NewGuid().ToString());
    var multi = await Expect("GET", "/api/posts/feed?pageSize=4", null, HttpStatusCode.OK);
    Check(multi.GetProperty("data").EnumerateArray().Select(Topic).SequenceEqual(new[] { "food", "travel", "food", "travel" }), "multiple topics alternate in requested order");
    await Expect("PUT", "/api/profiles/me/interests", new { topics = Array.Empty<string>(), version = 2 }, HttpStatusCode.OK, Guid.NewGuid().ToString());
    var reset = await Expect("GET", "/api/posts/feed?pageSize=3", null, HttpStatusCode.OK);
    Check(reset.GetProperty("data").EnumerateArray().Select(Topic).Distinct().Count() == 3, "clearing interests restores general diversity");
    var tokenA = client.DefaultRequestHeaders.Authorization;
    var second = await Expect("POST", "/api/auth/register", new { email = $"other_{suffix}@example.test",
        password = "LocalSmoke123!", username = "other_" + suffix, displayName = "Other user" }, HttpStatusCode.OK);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", second.GetProperty("accessToken").GetString());
    var isolated = await Expect("GET", "/api/profiles/me/interests", null, HttpStatusCode.OK);
    Check(isolated.GetProperty("version").GetInt32() == 0, "second account has independent preferences");
    var concurrent = await Task.WhenAll(ConcurrentPut("food"), ConcurrentPut("art"));
    Check(concurrent.Order().SequenceEqual(new[] { 200, 409 }), "concurrent first saves produce one winner and one conflict");
    var updates = await Task.WhenAll(ConcurrentPut("travel", 1), ConcurrentPut("music", 1));
    Check(updates.Order().SequenceEqual(new[] { 200, 409 }), "concurrent existing updates produce one winner and one conflict");
    var winner = await Expect("GET", "/api/profiles/me/interests", null, HttpStatusCode.OK);
    Check(winner.GetProperty("version").GetInt32() == 2, "concurrent existing updates increment version exactly once");
    client.DefaultRequestHeaders.Authorization = tokenA;
    var unchanged = await Expect("GET", "/api/profiles/me/interests", null, HttpStatusCode.OK);
    Check(unchanged.GetProperty("version").GetInt32() == 3 && unchanged.GetProperty("topics").GetArrayLength() == 0,
        "another account cannot overwrite first account preferences");
    foreach (var positive in new[] { "Like", "Love", "save" })
    {
        var account = await Expect("POST", "/api/auth/register", new {
            email = $"{positive}_{suffix}@example.test", password = "LocalSmoke123!",
            username = positive.ToLowerInvariant() + "_" + suffix, displayName = positive }, HttpStatusCode.OK);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", account.GetProperty("accessToken").GetString());
        Guid viewer;
        Guid ownId;
        await using (var db = new AppDbContext(options))
        {
            viewer = await db.Profiles.Where(p => p.Username == positive.ToLowerInvariant() + "_" + suffix).Select(p => p.Id).SingleAsync();
            var own = new Post { Id = Guid.NewGuid(), ProfileId = viewer, Caption = "warm-probe:" + positive,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Privacy = PrivacyLevel.Public };
            ownId = own.Id;
            db.Posts.Add(own);
            await db.SaveChangesAsync();
        }
        await Expect("PUT", "/api/profiles/me/interests", new { topics = new[] { "food" }, version = 0 }, HttpStatusCode.OK, Guid.NewGuid().ToString());
        var coldFeed = await Expect("GET", "/api/posts/feed?pageSize=100", null, HttpStatusCode.OK);
        var target = coldFeed.GetProperty("data")[0].GetProperty("id").GetGuid();
        await AssertOwn(false, positive + " before positive");
        Guid? savedCollectionId = null;
        if (positive == "Like")
        {
            await Expect("POST", $"/api/posts/{target}/reactions?type=Haha", null, HttpStatusCode.OK, Guid.NewGuid().ToString());
            await AssertOwn(false, "non-positive Haha remains cold");
        }
        if (positive == "save")
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("warm transition"), "Title");
            form.Add(new StringContent("Public"), "PrivacyLevel");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/collections") { Content = form };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            using var response = await client.SendAsync(request);
            Check(response.StatusCode == HttpStatusCode.OK, "save creates collection through HTTP");
            var collection = await response.Content.ReadFromJsonAsync<JsonElement>();
            savedCollectionId = collection.GetProperty("id").GetGuid();
            await Expect("POST", $"/api/collections/{collection.GetProperty("id").GetGuid()}/posts/{target}", null, HttpStatusCode.OK, Guid.NewGuid().ToString());
        }
        else
            await Expect("POST", $"/api/posts/{target}/reactions?type={positive}", null, HttpStatusCode.OK, Guid.NewGuid().ToString());
        await AssertOwn(true, positive + " after first positive");
        if (positive == "save")
            await Expect("DELETE", $"/api/collections/{savedCollectionId}/posts/{target}", null, HttpStatusCode.NoContent, Guid.NewGuid().ToString());
        else
            await Expect("POST", $"/api/posts/{target}/reactions?type={positive}", null, HttpStatusCode.OK, Guid.NewGuid().ToString());
        await AssertOwn(false, positive + " removed last positive returns cold");

        async Task AssertOwn(bool expected, string name)
        {
            foreach (var endpoint in new[] { "feed", "feed-with-reposts" })
            {
                var feed = await Expect("GET", $"/api/posts/{endpoint}?pageSize=100", null, HttpStatusCode.OK);
                var ids = feed.GetProperty("data").EnumerateArray().Select(p =>
                    (endpoint == "feed" ? p : p.GetProperty("post")).GetProperty("id").GetGuid());
                Check(ids.Contains(ownId) == expected, name + " switches correct path: " + endpoint);
            }
        }
    }
    Console.WriteLine($"PASS: {checks.Count} HTTP/status/behavior checks against Kestrel + PostgreSQL + Redis.");
}
finally
{
    if (!process.HasExited) process.Kill(entireProcessTree: true);
    await process.WaitForExitAsync();
    await File.WriteAllTextAsync(Path.Combine(logs, "api.log"), await stdout + await stderr);
    await File.WriteAllTextAsync(Path.Combine(logs, "checks.json"), JsonSerializer.Serialize(checks, new JsonSerializerOptions { WriteIndented = true }));
}

void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("FAIL: " + name);
    checks.Add(name);
    Console.WriteLine("PASS " + name);
}

async Task<JsonElement> Expect(string method, string path, object? body, HttpStatusCode status, string? key = null)
{
    using var request = new HttpRequestMessage(new HttpMethod(method), path);
    if (body != null) request.Content = JsonContent.Create(body);
    if (key != null) request.Headers.Add("Idempotency-Key", key);
    using var response = await client.SendAsync(request);
    var text = await response.Content.ReadAsStringAsync();
    if (response.StatusCode != status) throw new InvalidOperationException($"{method} {path}: expected {(int)status}, got {(int)response.StatusCode}: {text}");
    Check(true, $"{method} {path} -> {(int)status}");
    return string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
}

static string Topic(JsonElement post) => post.GetProperty("tags")[0].GetProperty("name").GetString()!;

async Task<int> ConcurrentPut(string topic, int version = 0)
{
    using var request = new HttpRequestMessage(HttpMethod.Put, "/api/profiles/me/interests")
        { Content = JsonContent.Create(new { topics = new[] { topic }, version }) };
    request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
    using var response = await client.SendAsync(request);
    return (int)response.StatusCode;
}

async Task<string> Snapshot(string table, bool excludeVersion)
{
    await using var connectionSnapshot = new NpgsqlConnection(connection);
    await connectionSnapshot.OpenAsync();
    // Only fixed table names from this runner are accepted.
    if (table is not ("Profiles" or "Posts")) throw new ArgumentException(nameof(table));
    var expression = excludeVersion ? "to_jsonb(t) - 'Version'" : "to_jsonb(t)";
    await using var command = new NpgsqlCommand($"SELECT jsonb_agg({expression} ORDER BY t.\"Id\")::text FROM \"{table}\" t", connectionSnapshot);
    return (string)(await command.ExecuteScalarAsync())!;
}
}
