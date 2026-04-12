using System.Collections.ObjectModel;

namespace Favi_BE.API.Seed;

public static class SeedConfig
{
    public const string SeedKey = "favi_v1";

    public static readonly SeedCountRange Users = new(5000, 5000);
    public static readonly SeedCountRange Posts = new(10000, 12000);
    public static readonly SeedCountRange Follows = new(50000, 70000);
    public static readonly SeedCountRange Reactions = new(80000, 120000);
    public static readonly SeedCountRange Comments = new(15000, 30000);
    public static readonly SeedCountRange Reposts = new(1000, 2000);
    public static readonly SeedCountRange Tags = new(50, 120);
    public static readonly SeedCountRange VectorizedPosts = new(3000, 5000);

    public const int ImageCatalogMinSize = 1000;

    public static readonly ReadOnlyDictionary<string, double> UserRoleDistribution =
        new(new Dictionary<string, double>
        {
            ["lurker"] = 0.70,
            ["casual"] = 0.25,
            ["power"] = 0.05
        });

    public static readonly SeedOutputPaths OutputPaths = new(
        Root: "seed-output",
        ManifestFileName: "seed-manifest.json",
        ImageCatalogFileName: "image-catalog.json",
        RunImageSetFileName: "run-image-set.json"
    );
}

public readonly record struct SeedCountRange(int Min, int Max);

public readonly record struct SeedOutputPaths(
    string Root,
    string ManifestFileName,
    string ImageCatalogFileName,
    string RunImageSetFileName
);
