using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetPostReactors;

public sealed record GetPostReactorsQuery(Guid PostId) : IQuery<IReadOnlyList<ReactorQueryDto>>;
