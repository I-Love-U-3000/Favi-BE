using Favi_BE.BuildingBlocks.Application.Messaging;

namespace Favi_BE.Modules.Auth.Application.Commands.UpdateLastActive;

public sealed record UpdateLastActiveCommand(Guid ProfileId) : ICommand;
