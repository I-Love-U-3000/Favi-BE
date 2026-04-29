using Favi_BE.Modules.Messaging.Application.Contracts;
using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Messaging.Application.Queries.GetConversations;

internal sealed class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, IReadOnlyList<ConversationSummaryReadModel>>
{
    private readonly IMessagingQueryReader _reader;

    public GetConversationsQueryHandler(IMessagingQueryReader reader) => _reader = reader;

    public Task<IReadOnlyList<ConversationSummaryReadModel>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, request.PageSize);
        return _reader.GetConversationsAsync(request.ProfileId, (page - 1) * pageSize, pageSize, cancellationToken);
    }
}
