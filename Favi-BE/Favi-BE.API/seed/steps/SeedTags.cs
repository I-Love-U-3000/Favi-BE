using System.Globalization;
using System.Text;
using System.Text.Json;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;

namespace Favi_BE.API.Seed.Steps;

public sealed class SeedTagsStep
{
    private static readonly string[] BaseTagNames =
    [
        "travel", "food", "sports", "music", "art", "tech", "nature", "fitness", "books", "gaming",
        "city", "lifestyle", "coding", "photography", "science", "history", "comedy", "design", "movies", "coffee"
    ];

    public async Task<SeedTagsResult> ExecuteAsync(
        AppDbContext db,
        IReadOnlyList<Post> posts,
        SeedContext seedContext,
        CancellationToken cancellationToken = default)
    {
        if (posts.Count == 0)
            throw new InvalidOperationException("Step 5 requires posts from Step 3.");

        var targetTagCount = seedContext.Random.Next(SeedConfig.Tags.Min, SeedConfig.Tags.Max + 1);
        var tags = GenerateTags(targetTagCount);
        var postTags = GeneratePostTags(posts, tags, seedContext);

        ValidateTags(tags, postTags, posts);

        await db.Tags.AddRangeAsync(tags, cancellationToken);
        await db.PostTags.AddRangeAsync(postTags, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var tagsPath = ExportTagsCsv(tags);
        var postTagsPath = ExportPostTagsCsv(postTags);

        return new SeedTagsResult(tags.Count, postTags.Count, tagsPath, postTagsPath);
    }

    private static List<Tag> GenerateTags(int targetTagCount)
    {
        var catalogTags = TryExtractCatalogTags();
        var uniqueNames = new List<string>(targetTagCount);

        if (catalogTags != null)
        {
            foreach (var tag in catalogTags.OrderBy(t => t))
            {
                if (uniqueNames.Count >= targetTagCount) break;
                if (!uniqueNames.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    uniqueNames.Add(tag);
            }
        }

        for (var i = 0; uniqueNames.Count < targetTagCount; i++)
        {
            var baseName = BaseTagNames[i % BaseTagNames.Length];
            var suffix = i / BaseTagNames.Length;
            var name = suffix == 0 ? baseName : $"{baseName}_{suffix:D2}";
            if (!uniqueNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                uniqueNames.Add(name);
        }

        return uniqueNames.Take(targetTagCount).Select(name => new Tag
        {
            Id = Guid.NewGuid(),
            Name = name
        }).ToList();
    }

    private static List<PostTag> GeneratePostTags(IReadOnlyList<Post> posts, IReadOnlyList<Tag> tags, SeedContext seedContext)
    {
        var postTags = new List<PostTag>(posts.Count * 2);
        var tagWeights = BuildTagWeights(tags.Count);
        var tagLookup = tags.ToDictionary(t => t.Name.ToLowerInvariant(), t => t);

        foreach (var post in posts)
        {
            var tagCountForPost = seedContext.Random.Next(1, 4);
            var selected = new HashSet<Guid>();

            if (!string.IsNullOrWhiteSpace(post.Caption))
            {
                var words = post.Caption.Split(new[] { ' ', ',', '.', '!', '?', '#' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var w in words)
                {
                    if (selected.Count >= tagCountForPost) break;
                    var clean = w.Trim().ToLowerInvariant();
                    if (tagLookup.TryGetValue(clean, out var matchedTag))
                    {
                        if (selected.Add(matchedTag.Id))
                        {
                            postTags.Add(new PostTag
                            {
                                PostId = post.Id,
                                TagId = matchedTag.Id
                            });
                        }
                    }
                }
            }

            while (selected.Count < tagCountForPost)
            {
                var tag = PickWeightedTag(tags, tagWeights, seedContext);
                if (!selected.Add(tag.Id))
                    continue;

                postTags.Add(new PostTag
                {
                    PostId = post.Id,
                    TagId = tag.Id
                });
            }
        }

        return postTags;
    }

    private static HashSet<string>? TryExtractCatalogTags()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "seed", "catalogs", "real-posts-catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "seed", "catalogs", "real-posts-catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Favi-BE", "Favi-BE.API", "seed", "catalogs", "real-posts-catalog.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "seed", "catalogs", "real-posts-catalog.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "seed", "catalogs", "real-posts-catalog.json")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Favi-BE.API", "seed", "catalogs", "real-posts-catalog.json"))
        };

        var catalogPath = candidates.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(catalogPath))
            return null;

        try
        {
            var json = File.ReadAllText(catalogPath);
            using var doc = JsonDocument.Parse(json);
            var tagSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                if (elem.TryGetProperty("tags", out var tagsElem) && tagsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in tagsElem.EnumerateArray())
                    {
                        var tagStr = t.GetString();
                        if (!string.IsNullOrWhiteSpace(tagStr))
                            tagSet.Add(tagStr.Trim().ToLowerInvariant());
                    }
                }
            }

            return tagSet.Count > 0 ? tagSet : null;
        }
        catch
        {
            return null;
        }
    }

    private static double[] BuildTagWeights(int count)
    {
        var weights = new double[count];
        for (var i = 0; i < count; i++)
        {
            var rank = i + 1d;
            weights[i] = 1d / Math.Pow(rank, 0.9d);
        }

        return weights;
    }

    private static Tag PickWeightedTag(IReadOnlyList<Tag> tags, IReadOnlyList<double> weights, SeedContext seedContext)
    {
        var totalWeight = weights.Sum();
        var roll = seedContext.Random.NextDouble() * totalWeight;

        for (var i = 0; i < tags.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0)
                return tags[i];
        }

        return tags[^1];
    }

    private static void ValidateTags(
        IReadOnlyCollection<Tag> tags,
        IReadOnlyCollection<PostTag> postTags,
        IReadOnlyList<Post> posts)
    {
        if (tags.Count < SeedConfig.Tags.Min || tags.Count > SeedConfig.Tags.Max)
            throw new InvalidOperationException("Validation failed: tags count is outside expected range.");

        var duplicateTagName = tags
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateTagName is not null)
            throw new InvalidOperationException($"Validation failed: duplicate tag name '{duplicateTagName.Key}'.");

        var duplicatePostTag = postTags
            .GroupBy(pt => new { pt.PostId, pt.TagId })
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicatePostTag is not null)
            throw new InvalidOperationException("Validation failed: duplicate post-tag pair detected.");

        var postIdSet = posts.Select(p => p.Id).ToHashSet();
        var tagIdSet = tags.Select(t => t.Id).ToHashSet();

        if (postTags.Any(pt => !postIdSet.Contains(pt.PostId) || !tagIdSet.Contains(pt.TagId)))
            throw new InvalidOperationException("Validation failed: post-tag has invalid foreign keys.");

        var postsWithoutTag = posts.Count - postTags.Select(pt => pt.PostId).Distinct().Count();
        if (postsWithoutTag > 0)
            Console.WriteLine($"[SeedTagsStep] WARNING: {postsWithoutTag} posts have no tag.");
    }

    private static string ExportTagsCsv(IEnumerable<Tag> tags)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);
        var filePath = Path.Combine(outputRoot, "tags.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("tag_id,name");
        foreach (var tag in tags)
        {
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{tag.Id},{tag.Name}"));
        }

        return filePath;
    }

    private static string ExportPostTagsCsv(IEnumerable<PostTag> postTags)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);
        var filePath = Path.Combine(outputRoot, "post-tags.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("post_id,tag_id");
        foreach (var postTag in postTags)
        {
            writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{postTag.PostId},{postTag.TagId}"));
        }

        return filePath;
    }
}

public readonly record struct SeedTagsResult(int CreatedTags, int CreatedPostTags, string TagsExportPath, string PostTagsExportPath);
