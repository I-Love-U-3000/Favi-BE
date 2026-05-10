using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Stories.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Stories.Application.Queries.GetActiveStoriesByProfile;

public sealed record GetActiveStoriesByProfileQuery(Guid ProfileId, Guid? ViewerId) : IQuery<IReadOnlyList<StoryReadModel>>;
