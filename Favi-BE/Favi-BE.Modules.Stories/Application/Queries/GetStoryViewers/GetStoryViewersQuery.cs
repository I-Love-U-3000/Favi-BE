using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Stories.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Stories.Application.Queries.GetStoryViewers;

public sealed record GetStoryViewersQuery(Guid StoryId, Guid RequesterId) : IQuery<IReadOnlyList<StoryViewerReadModel>>;
