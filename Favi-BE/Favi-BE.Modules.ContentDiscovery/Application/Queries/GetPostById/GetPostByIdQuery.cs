using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.ContentDiscovery.Application.Queries.GetPostById;

public sealed record GetPostByIdQuery(Guid PostId, Guid? ViewerId) : IQuery<PostReadModel?>;
