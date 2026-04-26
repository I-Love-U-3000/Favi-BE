using Favi_BE.Modules.Stories.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Commands.RecordStoryView;

public sealed record RecordStoryViewCommand(
    Guid StoryId,
    Guid ViewerId
) : IRequest<StoryCommandResult>;
