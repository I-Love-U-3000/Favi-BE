using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Engagement.Application.Queries.GetCollectionReactors;

public sealed record GetCollectionReactorsQuery(Guid CollectionId) : IQuery<IReadOnlyList<ReactorQueryDto>>;
