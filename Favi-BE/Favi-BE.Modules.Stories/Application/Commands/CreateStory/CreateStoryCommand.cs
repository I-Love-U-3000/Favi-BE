using Favi_BE.Modules.Stories.Application.Responses;
using Favi_BE.Modules.Stories.Domain;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Commands.CreateStory;

public sealed record CreateStoryCommand(
    Guid AuthorId,
    string? MediaUrl,
    string? MediaPublicId,
    int MediaWidth,
    int MediaHeight,
    string? MediaFormat,
    string? ThumbnailUrl,
    StoryPrivacy Privacy
) : IRequest<StoryCommandResult>;
