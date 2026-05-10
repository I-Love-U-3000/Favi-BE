using Favi_BE.Modules.Notifications.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Notifications.Application.Queries.GetUnreadNotificationCount;

internal sealed class GetUnreadNotificationCountQueryHandler
    : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly INotificationQueryReader _reader;

    public GetUnreadNotificationCountQueryHandler(INotificationQueryReader reader) => _reader = reader;

    public Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
        => _reader.GetUnreadCountAsync(request.RecipientId, cancellationToken);
}
