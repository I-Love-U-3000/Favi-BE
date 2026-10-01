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

    public static Guid DeterministicGuid(string seedKey, string entityType, int index)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{seedKey}:{entityType}:{index}"));
        var guidBytes = new byte[16];
        Array.Copy(bytes, guidBytes, guidBytes.Length);
        return new Guid(guidBytes);
    }
}
