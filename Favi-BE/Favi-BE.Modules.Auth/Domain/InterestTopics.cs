namespace Favi_BE.Modules.Auth.Domain;

public static class InterestTopics
{
    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new[]
    {
        "travel", "food", "sports", "city", "nature", "technology", "lifestyle", "art", "fitness", "music"
    });

    public static string[] Normalize(IEnumerable<string>? topics)
    {
        if (topics is null) throw new ArgumentException("Topics must be an array; use [] for general feed.");
        var values = topics.Take(11).ToArray();
        if (values.Length > 10) throw new ArgumentException("Choose at most 10 topics.");
        var result = values.Select(t => (t ?? "").Trim().ToLowerInvariant()).Distinct().ToArray();
        if (result.Any(t => !All.Contains(t))) throw new ArgumentException("Unknown topic; use the topic catalog.");
        return result;
    }
}
