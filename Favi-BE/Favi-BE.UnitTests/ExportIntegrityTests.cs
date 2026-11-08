using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Favi.OasisSmoke;

namespace Favi_BE.UnitTests;

public sealed class ExportIntegrityTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "favi-export-" + Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, string> hashes = new();
    private const string Snapshot = "{\n  \"users\": []\n}\n";
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    public ExportIntegrityTests()
    {
        Directory.CreateDirectory(root);
        foreach (var name in ExportIntegrity.RequiredFiles)
        {
            File.WriteAllText(Path.Combine(root, name), name == "snapshot.json" ? Snapshot : "fixture\n", new UTF8Encoding(false));
            hashes[name] = Hash(File.ReadAllBytes(Path.Combine(root, name)));
        }
        Manifest();
    }

    private void Manifest(string? logicalHash = null) => File.WriteAllText(Path.Combine(root, "manifest.json"),
        JsonSerializer.Serialize(new { schema_version = 1, files = hashes,
            snapshot_sha256 = logicalHash ?? Hash(Encoding.UTF8.GetBytes(Snapshot)) }));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AcceptsExactLfAndWindowsCrLfBytes(bool windows)
    {
        if (windows)
        {
            File.WriteAllText(Path.Combine(root, "snapshot.json"), Snapshot.Replace("\n", "\r\n"), new UTF8Encoding(false));
            hashes["snapshot.json"] = Hash(File.ReadAllBytes(Path.Combine(root, "snapshot.json")));
            Manifest();
        }
        var result = ExportIntegrity.Read(root);
        Assert.Equal(windows ? Snapshot.Replace("\n", "\r\n") : Snapshot, result.Snapshot);
        Assert.Equal("fixture\n", result.Sql);
    }

    public static IEnumerable<object[]> Files => ExportIntegrity.RequiredFiles.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Files))]
    public void RejectsChangedBytes(string name)
    {
        File.AppendAllText(Path.Combine(root, name), "changed");
        Assert.Contains("checksum mismatch", Assert.Throws<InvalidDataException>(() => ExportIntegrity.Read(root)).Message);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void RejectsOmittedCoverage(string name)
    {
        hashes.Remove(name);
        Manifest();
        Assert.Contains("Missing export manifest entry", Assert.Throws<InvalidDataException>(() => ExportIntegrity.Read(root)).Message);
    }

    [Fact]
    public void RejectsLogicalHashDriftEvenWhenRawHashMatches()
    {
        Manifest(new string('0', 64));
        Assert.Contains("Logical snapshot", Assert.Throws<InvalidDataException>(() => ExportIntegrity.Read(root)).Message);
    }

    [Fact]
    public void RejectsTraversalEntry()
    {
        hashes["../outside.sql"] = new string('0', 64);
        Manifest();
        Assert.Contains("Unexpected", Assert.Throws<InvalidDataException>(() => ExportIntegrity.Read(root)).Message);
    }

    [Fact]
    public void RejectsDuplicateEntry()
    {
        var path = Path.Combine(root, "manifest.json");
        var json = File.ReadAllText(path);
        File.WriteAllText(path, json.Replace("\"files\":{", "\"files\":{\"snapshot.json\":\"duplicate\","));
        Assert.Contains("duplicate", Assert.Throws<InvalidDataException>(() => ExportIntegrity.Read(root)).Message);
    }

    private void ProjectionManifest()
    {
        File.Delete(Path.Combine(root, "manifest.json"));
        File.WriteAllText(Path.Combine(root, "projection-manifest.json"), JsonSerializer.Serialize(new
        {
            mode = "explicit Favi subset of a preserved full OASIS world",
            files = hashes.Where(pair => pair.Key is "snapshot.json" or "favi-seed.sql")
                .ToDictionary(pair => pair.Key, pair => pair.Value)
        }));
    }

    [Fact]
    public void AcceptsNativeProjectionWithoutAdapterCsvs()
    {
        ProjectionManifest();
        foreach (var name in ExportIntegrity.RequiredFiles.Where(name => name.EndsWith(".csv")))
            File.Delete(Path.Combine(root, name));
        Assert.Equal((Snapshot, "fixture\n"), ExportIntegrity.Read(root));
    }

    [Theory]
    [InlineData("snapshot.json", false)]
    [InlineData("snapshot.json", true)]
    [InlineData("favi-seed.sql", false)]
    [InlineData("favi-seed.sql", true)]
    public void RejectsNativeChangedBytesOrMissingCoverage(string name, bool omit)
    {
        if (omit) hashes.Remove(name);
        ProjectionManifest();
        if (!omit) File.AppendAllText(Path.Combine(root, name), "changed");
        Assert.Throws<InvalidDataException>(() => ExportIntegrity.Read(root));
    }

    [Fact]
    public void RejectsAmbiguousFormatsRatherThanDowngradingAdapterCoverage()
    {
        ProjectionManifest();
        Manifest();
        Assert.Contains("Ambiguous", Assert.Throws<InvalidDataException>(() => ExportIntegrity.Read(root)).Message);
    }

    public void Dispose() => Directory.Delete(root, true);
}
