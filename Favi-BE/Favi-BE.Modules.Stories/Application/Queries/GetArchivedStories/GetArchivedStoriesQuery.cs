using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Stories.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Stories.Application.Queries.GetArchivedStories;

public sealed record GetArchivedStoriesQuery(Guid ProfileId) : IQuery<IReadOnlyList<StoryReadModel>>;
