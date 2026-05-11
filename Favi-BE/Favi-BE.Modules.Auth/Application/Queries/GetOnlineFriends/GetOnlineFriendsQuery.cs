using Favi_BE.Modules.Auth.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Queries.GetOnlineFriends;

public sealed record GetOnlineFriendsQuery(Guid ProfileId, int WithinLastMinutes)
    : IRequest<IReadOnlyList<ProfileReadModel>>;
