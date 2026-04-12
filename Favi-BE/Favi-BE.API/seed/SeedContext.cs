using System.Security.Cryptography;
using System.Text;

namespace Favi_BE.API.Seed;

public sealed class SeedContext
{
    public string SeedKey { get; }
    public Random Random { get; }

    public SeedContext(string seedKey)
    {
        SeedKey = seedKey;
        Random = new Random(StableSeed.FromString(seedKey));
    }
}

public static class StableSeed
{
    public static int FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Seed key is required.", nameof(value));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return BitConverter.ToInt32(bytes, 0);
    }
}
