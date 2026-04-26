using Favi_BE.Modules.Stories.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Commands.ExpireStory;

public sealed record ExpireStoryCommand(
    Guid StoryId
) : IRequest<StoryCommandResult>;
