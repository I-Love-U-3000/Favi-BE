using Favi_BE.Modules.Auth.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Auth.Application.Responses;

public sealed class ProfileCommandResult
{
    public bool Succeeded { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }
    public ProfileReadModel? Profile { get; private init; }

    public static ProfileCommandResult Success() => new() { Succeeded = true };

    public static ProfileCommandResult WithProfile(ProfileReadModel profile) =>
        new() { Succeeded = true, Profile = profile };

    public static ProfileCommandResult Fail(string code, string message) =>
        new() { Succeeded = false, ErrorCode = code, ErrorMessage = message };
}

public sealed class SavedImageResult
{
    public bool Succeeded { get; private init; }
    public string? ErrorCode { get; private init; }
    public Guid MediaId { get; private init; }
    public string? Url { get; private init; }
    public string? PublicId { get; private init; }
    public int Width { get; private init; }
    public int Height { get; private init; }
    public string? Format { get; private init; }
    public string? ThumbnailUrl { get; private init; }

    public static SavedImageResult Success(Guid mediaId, string url, string publicId, int width, int height, string format, string? thumbnailUrl) =>
        new() { Succeeded = true, MediaId = mediaId, Url = url, PublicId = publicId, Width = width, Height = height, Format = format, ThumbnailUrl = thumbnailUrl };

    public static SavedImageResult Fail(string code) =>
        new() { Succeeded = false, ErrorCode = code };
}
