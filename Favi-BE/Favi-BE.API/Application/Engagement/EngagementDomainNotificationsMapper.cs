using System.Text.Json;
using Favi_BE.BuildingBlocks.Application.Outbox;
using Favi_BE.BuildingBlocks.Domain;
using Favi_BE.Interfaces;
using Favi_BE.Modules.Engagement.Domain.Events;
using Favi_BE.Modules.Notifications.Domain.Events;

namespace Favi_BE.API.Application.Engagement;

/// <summary>
/// Maps Engagement domain events to integration events written to the outbox.
/// Resolves actor/recipient profile data and applies self-notification guard.
/// </summary>
internal sealed class EngagementDomainNotificationsMapper : IModuleDomainEventMapper
{
    private readonly IUnitOfWork _uow;

    public EngagementDomainNotificationsMapper(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public bool CanHandle(IDomainEvent domainEvent) =>
        domainEvent is CommentCreatedDomainEvent
        or PostReactionAddedDomainEvent
        or CommentReactionAddedDomainEvent;

    public Task<OutboxMessageData?> MapAsync(IDomainEvent domainEvent, string? correlationId, string? causationId) =>
        domainEvent switch
        {
            CommentCreatedDomainEvent e       => MapCommentCreatedAsync(e, correlationId, causationId),
            PostReactionAddedDomainEvent e    => MapPostReactionAsync(e, correlationId, causationId),
            CommentReactionAddedDomainEvent e => MapCommentReactionAsync(e, correlationId, causationId),
            _                                => Task.FromResult<OutboxMessageData?>(null)
        };

    private async Task<OutboxMessageData?> MapCommentCreatedAsync(
        CommentCreatedDomainEvent e, string? correlationId, string? causationId)
    {
        var post = await _uow.Posts.GetByIdAsync(e.PostId);
        if (post is null) return null;
        if (post.ProfileId == e.AuthorId) return null; // self-notification guard

        var actor = await _uow.Profiles.GetByIdAsync(e.AuthorId);
        if (actor is null) return null;

        var integrationEvent = new CommentCreatedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: e.OccurredOnUtc,
            AuthorId: e.AuthorId,
            PostId: e.PostId,
            CommentId: e.CommentId,
            RecipientId: post.ProfileId,
            Message: $"{actor.DisplayName ?? actor.Username} commented on your post",
            ActorUsername: actor.Username,
            ActorDisplayName: actor.DisplayName,
            ActorAvatarUrl: actor.AvatarUrl);

        return Wrap(integrationEvent, typeof(CommentCreatedIntegrationEvent), e.OccurredOnUtc, correlationId, causationId);
    }

    private async Task<OutboxMessageData?> MapPostReactionAsync(
        PostReactionAddedDomainEvent e, string? correlationId, string? causationId)
    {
        var post = await _uow.Posts.GetByIdAsync(e.PostId);
        if (post is null) return null;
        if (post.ProfileId == e.ActorId) return null; // self-notification guard

        var actor = await _uow.Profiles.GetByIdAsync(e.ActorId);
        if (actor is null) return null;

        var integrationEvent = new PostReactionToggledIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: e.OccurredOnUtc,
            ActorId: e.ActorId,
            PostId: e.PostId,
            RecipientId: post.ProfileId,
            Message: $"{actor.DisplayName ?? actor.Username} reacted to your post",
            ActorUsername: actor.Username,
            ActorDisplayName: actor.DisplayName,
            ActorAvatarUrl: actor.AvatarUrl);

        return Wrap(integrationEvent, typeof(PostReactionToggledIntegrationEvent), e.OccurredOnUtc, correlationId, causationId);
    }

    private async Task<OutboxMessageData?> MapCommentReactionAsync(
        CommentReactionAddedDomainEvent e, string? correlationId, string? causationId)
    {
        var comment = await _uow.Comments.GetByIdAsync(e.CommentId);
        if (comment is null) return null;
        if (comment.ProfileId == e.ActorId) return null; // self-notification guard

        var actor = await _uow.Profiles.GetByIdAsync(e.ActorId);
        if (actor is null) return null;

        var integrationEvent = new CommentReactionToggledIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: e.OccurredOnUtc,
            ActorId: e.ActorId,
            CommentId: e.CommentId,
            TargetPostId: comment.PostId,
            RecipientId: comment.ProfileId,
            Message: $"{actor.DisplayName ?? actor.Username} reacted to your comment",
            ActorUsername: actor.Username,
            ActorDisplayName: actor.DisplayName,
            ActorAvatarUrl: actor.AvatarUrl);

        return Wrap(integrationEvent, typeof(CommentReactionToggledIntegrationEvent), e.OccurredOnUtc, correlationId, causationId);
    }

    private static OutboxMessageData Wrap<TEvent>(
        TEvent evt, Type type, DateTime occurredOnUtc, string? correlationId, string? causationId)
        where TEvent : notnull =>
        new(
            Id: Guid.NewGuid(),
            OccurredOnUtc: occurredOnUtc,
            Type: type.FullName!,
            Payload: JsonSerializer.Serialize(evt, type),
            CorrelationId: correlationId,
            CausationId: causationId);
}
