using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetRepostById;

public sealed record GetRepostByIdQuery(Guid RepostId, Guid? ViewerId) : IQuery<RepostReadModel?>;
