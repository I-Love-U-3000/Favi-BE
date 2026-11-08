using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Favi.OasisSmoke;

// Validate the copied export, then consume the exact bytes that were checked.
internal static class ExportIntegrity
{
    internal static readonly string[] RequiredFiles = ["snapshot.json", "favi-seed.sql", "users.csv",
        "posts.csv", "user_post_edges.csv", "user_user_edges.csv"];

    internal static (string Snapshot, string Sql) Read(string root)
    {
        var adapterPath = Path.Combine(root, "manifest.json");
        var projectionPath = Path.Combine(root, "projection-manifest.json");
        var projection = File.Exists(projectionPath);
        if (projection && File.Exists(adapterPath))
            throw new InvalidDataException("Ambiguous export manifest formats");
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(projection ? projectionPath : adapterPath));
        var value = manifest.RootElement;
        if (projection && value.GetProperty("mode").GetString() != "explicit Favi subset of a preserved full OASIS world")
            throw new InvalidDataException("Unsupported projection manifest mode");
        if (!projection && value.GetProperty("schema_version").GetInt32() != 1)
            throw new InvalidDataException("Unsupported adapter manifest schema");
        string[] required = projection ? ["snapshot.json", "favi-seed.sql"] : RequiredFiles;
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in value.GetProperty("files").EnumerateObject())
        {
            if (!required.Contains(entry.Name, StringComparer.Ordinal) || !files.TryAdd(entry.Name, entry.Value.GetString()!))
                throw new InvalidDataException("Unexpected or duplicate export manifest entry: " + entry.Name);
        }
        var payloads = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var name in required)
        {
            if (!files.TryGetValue(name, out var expected))
                throw new InvalidDataException("Missing export manifest entry: " + name);
            var bytes = File.ReadAllBytes(Path.Combine(root, name));
            if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Export checksum mismatch: " + name);
            payloads.Add(name, bytes);
        }
        var utf8 = new UTF8Encoding(false, true);
        var snapshot = utf8.GetString(payloads["snapshot.json"]);
        // Adapter hashes the logical payload before platform newline translation.
        var logical = snapshot.Replace("\r\n", "\n").Replace('\r', '\n');
        if (!projection && !string.Equals(Convert.ToHexString(SHA256.HashData(utf8.GetBytes(logical))),
            value.GetProperty("snapshot_sha256").GetString(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Logical snapshot checksum mismatch");
        return (snapshot, utf8.GetString(payloads["favi-seed.sql"]));
    }
}
