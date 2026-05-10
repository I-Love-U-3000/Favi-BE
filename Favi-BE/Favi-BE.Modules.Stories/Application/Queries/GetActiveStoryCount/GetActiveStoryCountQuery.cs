using Favi_BE.BuildingBlocks.Application.Messaging;

namespace Favi_BE.Modules.Stories.Application.Queries.GetActiveStoryCount;

public sealed record GetActiveStoryCountQuery(Guid ProfileId) : IQuery<int>;
