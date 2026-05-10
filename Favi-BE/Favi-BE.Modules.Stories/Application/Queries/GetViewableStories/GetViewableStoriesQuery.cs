using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Stories.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Stories.Application.Queries.GetViewableStories;

public sealed record GetViewableStoriesQuery(Guid ViewerId) : IQuery<IReadOnlyList<StoryReadModel>>;
