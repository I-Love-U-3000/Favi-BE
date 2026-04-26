using Favi_BE.Modules.Stories.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Commands.DeleteStory;

public sealed record DeleteStoryCommand(
    Guid StoryId,
    Guid RequesterId
) : IRequest<StoryCommandResult>;
