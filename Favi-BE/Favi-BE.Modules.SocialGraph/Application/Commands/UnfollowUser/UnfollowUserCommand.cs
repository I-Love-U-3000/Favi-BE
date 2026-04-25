using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.SocialGraph.Application.Responses;

namespace Favi_BE.Modules.SocialGraph.Application.Commands.UnfollowUser;

public sealed record UnfollowUserCommand(
    Guid FollowerId,
    Guid FolloweeId) : ICommand<FollowCommandResult>;
