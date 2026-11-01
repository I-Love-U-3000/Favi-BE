using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

try { await RunAsync(args); }
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }

static async Task RunAsync(string[] args)
{
    if (args.Length != 1) throw new ArgumentException("Pass the fixture export directory");
    var exportRoot = Path.GetFullPath(args[0]);
    var snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(exportRoot, "snapshot.json"))).RootElement.Clone();
    var sql = await File.ReadAllTextAsync(Path.Combine(exportRoot, "favi-seed.sql"));
    var root = Directory.GetCurrentDirectory();
    var apiDir = Path.Combine(root, "Favi-BE.API");
    var logs = Path.Combine(Path.GetDirectoryName(exportRoot)!, "report");
    Directory.CreateDirectory(logs);
    var checks = new List<string>();
    var database = "oasis_import_" + Guid.NewGuid().ToString("N");
    var rollbackDatabase = "oasis_rollback_" + Guid.NewGuid().ToString("N");
    const string server = "Host=127.0.0.1;Port=15449;Username=oasis_smoke;Password=local_oasis_only";
    string Connection(string name) => server + ";Database=" + name;
    // Read-only startup readiness from the host, not just container-local health.
    Exception? readinessError = null;
    for (var attempt = 0; attempt < 15; attempt++)
    {
        try
        {
            await using var probe = new NpgsqlConnection(Connection("postgres") + ";Timeout=2;Command Timeout=2;Pooling=false");
            await probe.OpenAsync();
            await using var select = new NpgsqlCommand("SELECT 1", probe);
            await select.ExecuteScalarAsync();
            readinessError = null;
            break;
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException)
        {
            readinessError = error;
            await Task.Delay(500);
        }
    }
    if (readinessError != null) throw new InvalidOperationException("Test PostgreSQL did not become ready from host", readinessError);
    foreach (var name in new[] { database, rollbackDatabase })
    {
        await using var admin = new NpgsqlConnection(Connection("postgres"));
        await admin.OpenAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin);
        await create.ExecuteNonQueryAsync();
        await using var db = Db(name);
        await db.Database.MigrateAsync();
    }

    Process? process = null;
    Task<string>? stdout = null, stderr = null;
    var passed = false;
    string? apiUrl = null;
    try
    {
        var sentinelId = Guid.NewGuid();
        await using (var db = Db(rollbackDatabase))
        {
            db.Profiles.Add(new Profile { Id = sentinelId, Username = "preexisting_sentinel", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        var brokenSql = sql.Replace("COMMIT;", """
            INSERT INTO "Follows" ("FollowerId", "FolloweeId", "CreatedAt")
            VALUES ('ffffffff-ffff-ffff-ffff-ffffffffffff','eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',NOW());
            COMMIT;
            """);
        var failedWithFk = false;
        try { await Import(rollbackDatabase, brokenSql); }
        catch (PostgresException error) when (error.SqlState == "23503") { failedWithFk = true; }
        Check(failedWithFk, "injected FK failure is rejected by real PostgreSQL");
        await using (var db = Db(rollbackDatabase))
        {
            Check(await db.Profiles.CountAsync() == 1 && await db.Profiles.AnyAsync(p => p.Id == sentinelId), "failed import preserves preexisting profile only");
            Check(await db.Posts.CountAsync() == 0 && await db.PostMedias.CountAsync() == 0 &&
                await db.Reactions.CountAsync() == 0 && await db.Comments.CountAsync() == 0 && await db.Follows.CountAsync() == 0,
                "failed import rolls back all posts media reactions comments follows");
        }

        await Import(database, sql);
        var beforeReplay = await DatabaseSnapshot();
        await Import(database, sql);
        Check(beforeReplay == await DatabaseSnapshot(), "SQL replay preserves every imported field without duplicates");
        await using (var db = Db(database))
        {
            Check(await db.Profiles.CountAsync() == snapshot.GetProperty("users").GetArrayLength() && await db.Posts.CountAsync() == snapshot.GetProperty("posts").GetArrayLength() && await db.PostMedias.CountAsync() == snapshot.GetProperty("posts").GetArrayLength(),
                "profile post and media counts match snapshot");
            Check(await db.Reactions.CountAsync() == snapshot.GetProperty("likes").GetArrayLength() && await db.Follows.CountAsync() == snapshot.GetProperty("follows").GetArrayLength() && await db.Comments.CountAsync() == snapshot.GetProperty("comments").GetArrayLength(),
                "reaction follow and comment counts match snapshot");
            foreach (var user in snapshot.GetProperty("users").EnumerateArray())
            {
                var row = await db.Profiles.AsNoTracking().SingleAsync(p => p.Id == user.GetProperty("id").GetGuid());
                Check(row.Username == user.GetProperty("username").GetString() && row.DisplayName == user.GetProperty("display_name").GetString(), "source profile GUID and Unicode survive PostgreSQL import");
            }
            foreach (var post in snapshot.GetProperty("posts").EnumerateArray())
            {
                var row = await db.Posts.Include(p => p.PostMedias).SingleAsync(p => p.Id == post.GetProperty("id").GetGuid());
                Check(row.ProfileId == post.GetProperty("user_id").GetGuid(), "post author matches snapshot");
                Check(row.Caption == post.GetProperty("content").GetString(), "post caption matches snapshot");
                Check(row.IsNSFW == post.GetProperty("is_nsfw").GetBoolean(), "post NSFW flag matches snapshot");
                Check(row.CreatedAt == post.GetProperty("created_at").GetDateTimeOffset().UtcDateTime, "post UTC timestamp matches snapshot");
                Check(row.PostMedias.Single().Url == post.GetProperty("image_url").GetString(), "media URL matches snapshot");
            }
            foreach (var like in snapshot.GetProperty("likes").EnumerateArray())
            {
                var row = await db.Reactions.SingleAsync(r => r.Id == like.GetProperty("id").GetGuid());
                Check(row.ProfileId == like.GetProperty("user_id").GetGuid() && row.PostId == like.GetProperty("post_id").GetGuid() &&
                    (int)row.Type == 0 && row.CreatedAt == like.GetProperty("created_at").GetDateTimeOffset().UtcDateTime, "like identity endpoints type and timestamp match snapshot");
            }
            foreach (var follow in snapshot.GetProperty("follows").EnumerateArray())
            {
            var followRow = await db.Follows.SingleAsync(f => f.FollowerId == follow.GetProperty("follower_id").GetGuid() && f.FolloweeId == follow.GetProperty("followee_id").GetGuid());
            Check(followRow.FollowerId == follow.GetProperty("follower_id").GetGuid() && followRow.FolloweeId == follow.GetProperty("followee_id").GetGuid(), "follow direction matches snapshot");
            }
            foreach (var comment in snapshot.GetProperty("comments").EnumerateArray())
            {
            var commentRow = await db.Comments.SingleAsync(c => c.Id == comment.GetProperty("id").GetGuid());
            Check(commentRow.Id == comment.GetProperty("id").GetGuid() && commentRow.ProfileId == comment.GetProperty("user_id").GetGuid() &&
                commentRow.PostId == comment.GetProperty("post_id").GetGuid(), "comment identity author and post match snapshot");
            Check(commentRow.Content == comment.GetProperty("content").GetString(), "source comment is preserved as text");
            }
        }
        var safePosts = snapshot.GetProperty("posts").EnumerateArray().Where(p => !p.GetProperty("is_nsfw").GetBoolean()).ToArray();
        var suffix = Guid.NewGuid().ToString("N");
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = apiDir, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add(Path.Combine(apiDir, "bin", "Debug", "net9.0", "Favi-BE.API.dll"));
        foreach (var pair in new Dictionary<string, string>
        {
            ["ASPNETCORE_URLS"] = "http://127.0.0.1:0", ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information",
            ["ConnectionStrings__Default"] = Connection(database), ["Redis__ConnectionString"] = "127.0.0.1:16399,abortConnect=false",
            ["Jwt__Key"] = "local-oasis-test-only-signing-key-123456789012345678901234567890123456",
            ["Jwt__Issuer"] = "oasis-smoke", ["Jwt__Audience"] = "oasis-smoke",
            ["CloudinarySettings__CloudName"] = "smoke", ["CloudinarySettings__ApiKey"] = "smoke", ["CloudinarySettings__ApiSecret"] = "smoke",
            ["Seeding__Enabled"] = "false", ["VectorIndex__Enabled"] = "false", ["NSFW__Enabled"] = "false"
        }) start.Environment[pair.Key] = pair.Value;
        process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start test API");
        var listening = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
        stdout = CaptureOutput(process, listening); stderr = process.StandardError.ReadToEndAsync();
        var address = await listening.Task.WaitAsync(TimeSpan.FromSeconds(30));
        apiUrl = address.ToString();
        using var client = new HttpClient { BaseAddress = address, Timeout = TimeSpan.FromSeconds(20) };
        for (var attempt = 0; ; attempt++)
        {
            if (process.HasExited) throw new InvalidOperationException("Test API exited before readiness");
            try { if ((await client.GetAsync("/health/live")).IsSuccessStatusCode && !process.HasExited) break; } catch (HttpRequestException) { }
            if (attempt >= 60 || process.HasExited) throw new InvalidOperationException("API did not start; see api.log");
            await Task.Delay(500);
        }
        var registration = await Request("POST", "/api/auth/register", new { email = $"oasis_{suffix}@example.test", password = "LocalSmoke123!", username = "viewer_" + suffix, displayName = "Oasis viewer" });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registration.GetProperty("accessToken").GetString());
        foreach (var user in snapshot.GetProperty("users").EnumerateArray())
        {
        var authorId = user.GetProperty("id").GetGuid();
        var profile = await Request("GET", $"/api/profiles/{authorId}");
        Check(profile.GetProperty("displayName").GetString() == user.GetProperty("display_name").GetString(), "imported profile is readable through HTTP");
        var followers = await ReadPages($"/api/profiles/{authorId}/followers");
        var expectedFollowers = snapshot.GetProperty("follows").EnumerateArray().Where(f => f.GetProperty("followee_id").GetGuid() == authorId).ToArray();
        Check(followers.Select(f => f.GetProperty("followerId").GetGuid()).ToHashSet().SetEquals(expectedFollowers.Select(f => f.GetProperty("follower_id").GetGuid())) && followers.Count == expectedFollowers.Length, "imported follower identities and count are readable through paged HTTP");
        }
        foreach (var safe in safePosts)
        {
        var safeId = safe.GetProperty("id").GetGuid();
        var postResponse = await Request("GET", $"/api/posts/{safeId}");
        Check(postResponse.GetProperty("caption").GetString() == safe.GetProperty("content").GetString(), "imported post caption is readable through HTTP");
        Check(postResponse.GetProperty("medias")[0].GetProperty("url").GetString() == safe.GetProperty("image_url").GetString(), "HTTP media metadata matches imported URL");
        Check(postResponse.GetProperty("reactions").GetProperty("total").GetInt32() == snapshot.GetProperty("likes").EnumerateArray().Count(l => l.GetProperty("post_id").GetGuid() == safeId), "imported like is reflected in HTTP reaction summary");
        var comments = await ReadPages($"/api/comments/post/{safeId}");
        var expectedComments = snapshot.GetProperty("comments").EnumerateArray().Where(c => c.GetProperty("post_id").GetGuid() == safeId).ToArray();
        var actualComments = comments.ToArray();
        Check(actualComments.Length == expectedComments.Length, "imported comment count is readable through paged HTTP");
        foreach (var expected in expectedComments)
            Check(actualComments.Any(c => c.GetProperty("id").GetGuid() == expected.GetProperty("id").GetGuid() && c.GetProperty("content").GetString() == expected.GetProperty("content").GetString()), "HTTP preserves comment identity and literal content");
        }
        foreach (var endpoint in new[] { "feed", "feed-with-reposts" })
        {
            var feed = await ReadPages($"/api/posts/{endpoint}");
            var posts = feed.Select(p => endpoint == "feed" ? p : p.GetProperty("post")).ToArray();
            var ids = posts.Select(p => p.GetProperty("id").GetGuid()).ToHashSet();
            var safeIds = safePosts.Select(p => p.GetProperty("id").GetGuid()).ToHashSet();
            Check(posts.All(p => !p.GetProperty("isNSFW").GetBoolean()) && ids.Count == posts.Length && ids.IsSubsetOf(safeIds), endpoint + " has unique safe imported IDs across pages");
            // Production feeds have bounded candidate pools; they are not catalog exports.
            Check(safePosts.Length <= 60 ? ids.SetEquals(safeIds) : posts.Length > 0, endpoint + " exposes the full small catalog or a nonempty bounded large-catalog slate");
        }
        passed = true;
        Console.WriteLine($"PASS: {checks.Count} PostgreSQL/import/HTTP checks; see source provenance beside snapshot.");

        async Task<List<JsonElement>> ReadPages(string path)
        {
            var result = new List<JsonElement>();
            int? total = null;
            for (var page = 1; page <= 10000; page++)
            {
                var response = await Request("GET", $"{path}?page={page}&pageSize=100");
                var count = response.GetProperty("totalCount").GetInt32();
                Check(total is null || total == count, "paged response total remains stable");
                total = count;
                var rows = response.GetProperty("data").EnumerateArray().Select(r => r.Clone()).ToArray();
                result.AddRange(rows);
                if (!response.GetProperty("hasNext").GetBoolean())
                {
                    Check(result.Count == total, "paged response contains every advertised row");
                    return result;
                }
                if (rows.Length == 0) throw new InvalidOperationException("Empty page with hasNext=true");
            }
            throw new InvalidOperationException("Pagination exceeded verification bound");
        }

        async Task<JsonElement> Request(string method, string path, object? body = null)
        {
            if (process.HasExited) throw new InvalidOperationException("Refusing request after test API exited");
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (body != null) request.Content = JsonContent.Create(body);
            using var response = await client.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException($"{method} {path}: {(int)response.StatusCode}");
            Check(true, method + " " + path + " -> 200");
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).Clone();
        }
    }
    finally
    {
        if (process != null)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            await File.WriteAllTextAsync(Path.Combine(logs, "api.log"), await stdout! + await stderr!);
            process.Dispose();
        }
        await File.WriteAllTextAsync(Path.Combine(logs, "checks.json"), JsonSerializer.Serialize(new { passed, database, rollbackDatabase, apiUrl, checks }, new JsonSerializerOptions { WriteIndented = true }));
    }

    AppDbContext Db(string name) => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(Connection(name)).Options);
    void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("FAIL: " + name); checks.Add(name); Console.WriteLine("PASS " + name); }
    async Task Import(string name, string script)
    {
        await using var connection = new NpgsqlConnection(Connection(name)); await connection.OpenAsync();
        await using var command = new NpgsqlCommand(script, connection); await command.ExecuteNonQueryAsync();
    }
    async Task<string> DatabaseSnapshot()
    {
        var rows = new List<string>();
        await using var connection = new NpgsqlConnection(Connection(database)); await connection.OpenAsync();
        foreach (var table in new[] { "Profiles", "Posts", "PostMedias", "Reactions", "Follows", "Comments" })
        {
            await using var command = new NpgsqlCommand($"SELECT jsonb_agg(row ORDER BY row::text)::text FROM (SELECT to_jsonb(t) AS row FROM \"{table}\" t) source", connection);
            rows.Add((await command.ExecuteScalarAsync()) as string ?? "null");
        }
        return string.Join('\n', rows);
    }
    static async Task<string> CaptureOutput(Process child, TaskCompletionSource<Uri> listening)
    {
        var output = new StringBuilder();
        while (await child.StandardOutput.ReadLineAsync() is { } line)
        {
            output.AppendLine(line);
            // Kestrel binds port 0 atomically. Accept only the address announced by
            // this child process, never a health response on a guessed/shared port.
            var match = Regex.Match(line, @"Now listening on:\s+(http://127\.0\.0\.1:\d+)");
            if (match.Success) listening.TrySetResult(new Uri(match.Groups[1].Value));
        }
        listening.TrySetException(new InvalidOperationException("Test API never announced its listening address"));
        return output.ToString();
    }
}
