using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetPostsBatch;

public sealed record GetPostsBatchQuery(IReadOnlyList<Guid> PostIds, Guid? ViewerId) : IQuery<IReadOnlyList<PostReadModel>>;
