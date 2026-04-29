using Favi_BE.Modules.Messaging.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Messaging.Application.Queries.GetUnreadMessagesCount;

internal sealed class GetUnreadMessagesCountQueryHandler : IRequestHandler<GetUnreadMessagesCountQuery, int>
{
    private readonly IMessagingQueryReader _reader;

    public GetUnreadMessagesCountQueryHandler(IMessagingQueryReader reader) => _reader = reader;

    public Task<int> Handle(GetUnreadMessagesCountQuery request, CancellationToken cancellationToken)
        => _reader.GetUnreadMessagesCountAsync(request.ProfileId, cancellationToken);
}
