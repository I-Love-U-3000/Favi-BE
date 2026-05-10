using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Stories.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Stories.Application.Queries.GetStoryById;

public sealed record GetStoryByIdQuery(Guid StoryId, Guid? ViewerId) : IQuery<StoryReadModel?>;
