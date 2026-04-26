using Favi_BE.Modules.Stories.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Commands.ArchiveStory;

public sealed record ArchiveStoryCommand(
    Guid StoryId,
    Guid RequesterId
) : IRequest<StoryCommandResult>;
