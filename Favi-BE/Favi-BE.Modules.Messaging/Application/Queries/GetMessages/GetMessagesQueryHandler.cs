using Favi_BE.Modules.Messaging.Application.Contracts;
using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Messaging.Application.Queries.GetMessages;

internal sealed class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, (IReadOnlyList<MessageReadModel> Items, int Total)>
{
    private readonly IMessagingQueryReader _reader;

    public GetMessagesQueryHandler(IMessagingQueryReader reader) => _reader = reader;

    public Task<(IReadOnlyList<MessageReadModel> Items, int Total)> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, request.PageSize);
        return _reader.GetMessagesAsync(request.ConversationId, request.RequestingProfileId, (page - 1) * pageSize, pageSize, cancellationToken);
    }
}
