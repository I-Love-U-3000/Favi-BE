using Favi_BE.API.Models.Dtos;
using Favi_BE.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Favi_BE.API.Hubs
{
    [Authorize]
    public class CallHub : Hub
    {
        private readonly ILogger<CallHub> _logger;
        private readonly IChatService _chatService;
        private readonly IServiceProvider _serviceProvider;

        // Track active calls: conversationId -> (callerId, calleeId, startTime)
        private static readonly Dictionary<string, (string callerId, string calleeId, DateTime startTime)> _activeCalls = new();

        // Track WebRTC offers: conversationId -> (fromUserId, sdpOffer)
        private static readonly Dictionary<string, (string fromUserId, string sdpOffer)> _pendingOffers = new();

        public CallHub(ILogger<CallHub> logger, IChatService chatService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _chatService = chatService;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Join a call room for a specific conversation
        /// </summary>
        public async Task JoinCallRoom(string conversationId)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId) && Guid.TryParse(conversationId, out var convId))
            {
                try
                {
                    // Verify user has access to conversation
                    var conversations = await _chatService.GetConversationsAsync(userId, 1, 100);
                    var hasAccess = conversations.Any(c => c.Id == convId);

                    if (hasAccess)
                    {
                        var groupName = $"call_{convId}";
                        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                        _logger.LogInformation($"User {userId} joined call room {convId}");
                    }
                    else
                    {
                        _logger.LogWarning($"User {userId} denied access to call room {convId}");
                        await Clients.Caller.SendAsync("CallError", new { conversationId = convId, error = "Access denied" });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error joining call room {convId} for user {userId}");
                }
            }
        }

        /// <summary>
        /// Leave a call room
        /// </summary>
        public async Task LeaveCallRoom(string conversationId)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId) && Guid.TryParse(conversationId, out var convId))
            {
                try
                {
                    var groupName = $"call_{convId}";
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                    // Notify others in the call room
                    await Clients.OthersInGroup(groupName).SendAsync("UserLeftCall", new CallUserLeftDto(
                        convId.ToString(),
                        userId.ToString(),
                        "left"
                    ));

                    _logger.LogInformation($"User {userId} left call room {convId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error leaving call room {convId} for user {userId}");
                }
            }
        }

        /// <summary>
        /// Initiate a call to another user
        /// </summary>
        public async Task StartCall(string conversationId, string targetUserId, string callType)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            _logger.LogInformation($"[CallHub] StartCall invoked - UserIdentifier: {userIdStr}, conversationId: {conversationId}, targetUserId: {targetUserId}, callType: {callType}");

            if (Guid.TryParse(userIdStr, out var callerId) &&
                Guid.TryParse(conversationId, out var convId) &&
                Guid.TryParse(targetUserId, out var calleeId))
            {
                try
                {
                    _logger.LogInformation($"[CallHub] Parsed IDs - Caller: {callerId}, Callee: {calleeId}, Conversation: {convId}");

                    // Verify caller has access to conversation
                    var conversations = await _chatService.GetConversationsAsync(callerId, 1, 100);
                    var conversation = conversations.FirstOrDefault(c => c.Id == convId);

                    if (conversation == null)
                    {
                        _logger.LogWarning($"[CallHub] Conversation {convId} not found for user {callerId}");
                        await Clients.Caller.SendAsync("CallError", new { conversationId = convId, error = "Conversation not found" });
                        return;
                    }

                    // Get caller info
                    var caller = conversation.Members.FirstOrDefault(m => m.ProfileId == callerId);
                    if (caller == null)
                    {
                        _logger.LogWarning($"[CallHub] Caller {callerId} not found in conversation {convId}");
                        await Clients.Caller.SendAsync("CallError", new { conversationId = convId, error = "Caller not in conversation" });
                        return;
                    }

                    // Track the call
                    _activeCalls[convId.ToString()] = (callerId.ToString(), calleeId.ToString(), DateTime.UtcNow);

                    _logger.LogInformation($"[CallHub] Sending IncomingCall to user {calleeId} (as string: '{calleeId.ToString()}')");
                    _logger.LogInformation($"[CallHub] Caller info - Username: {caller.Username}, DisplayName: {caller.DisplayName}");

                    // Send incoming call notification to the target user
                    await Clients.User(calleeId.ToString()).SendAsync("IncomingCall", new IncomingCallRequestDto(
                        convId.ToString(),
                        callerId.ToString(),
                        caller.Username,
                        caller.DisplayName,
                        caller.AvatarUrl,
                        callType
                    ));

                    _logger.LogInformation($"[CallHub] User {callerId} started {callType} call to {calleeId} in conversation {convId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[CallHub] Error starting call in conversation {convId} by user {callerId}");
                    await Clients.Caller.SendAsync("CallError", new { conversationId = convId, error = "Failed to start call" });
                }
            }
            else
            {
                _logger.LogError($"[CallHub] Failed to parse IDs - userIdStr: {userIdStr}, conversationId: {conversationId}, targetUserId: {targetUserId}");
            }
        }

        /// <summary>
        /// Accept an incoming call
        /// </summary>
        public async Task AcceptCall(string conversationId, string callerId)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var calleeId) && Guid.TryParse(conversationId, out var convId))
            {
                try
                {
                    var groupName = $"call_{convId}";

                    // Notify caller that call was accepted
                    await Clients.User(callerId).SendAsync("CallAccepted", new CallResponseDto(
                        convId.ToString(),
                        "accept",
                        null
                    ));

                    // Notify both parties to join the call room
                    await Clients.User(callerId).SendAsync("JoinCallRoom", convId.ToString());
                    await Clients.Caller.SendAsync("JoinCallRoom", convId.ToString());

                    // CRITICAL FIX: Forward the stored WebRTC offer to the callee
                    var convIdStr = convId.ToString();
                    if (_pendingOffers.TryGetValue(convIdStr, out var pendingOffer))
                    {
                        _logger.LogInformation($"[CallHub] Forwarding stored offer from {pendingOffer.fromUserId} to callee {calleeId}");

                        await Clients.Caller.SendAsync("ReceiveOffer", new CallSignalDto(
                            pendingOffer.fromUserId,
                            callerId,
                            convIdStr,
                            "offer",
                            "offer",
                            pendingOffer.sdpOffer
                        ));

                        // Clean up the stored offer after forwarding
                        _pendingOffers.Remove(convIdStr);
                    }
                    else
                    {
                        _logger.LogWarning($"[CallHub] No pending offer found for conversation {convIdStr}");
                    }

                    _logger.LogInformation($"User {calleeId} accepted call from {callerId} in conversation {convId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error accepting call in conversation {convId} by user {calleeId}");
                }
            }
        }

        /// <summary>
        /// Reject an incoming call
        /// </summary>
        public async Task RejectCall(string conversationId, string callerId, string? reason = null)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var calleeId) && Guid.TryParse(conversationId, out var convId))
            {
                try
                {
                    // Remove from active calls
                    var convIdStr = convId.ToString();
                    _activeCalls.Remove(convIdStr);
                    _pendingOffers.Remove(convIdStr);

                    // Notify caller that call was rejected
                    await Clients.User(callerId).SendAsync("CallRejected", new CallResponseDto(
                        convIdStr,
                        "reject",
                        reason ?? "Call declined"
                    ));

                    _logger.LogInformation($"User {calleeId} rejected call from {callerId} in conversation {convId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error rejecting call in conversation {convId} by user {calleeId}");
                }
            }
        }

        /// <summary>
        /// Send WebRTC offer (SDP)
        /// </summary>
        public async Task SendOffer(string conversationId, string targetUserId, string sdpOffer)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var fromUserId) && Guid.TryParse(conversationId, out var convId))
            {
                try
                {
                    // Store the offer so it can be sent to the callee when they accept
                    _pendingOffers[convId.ToString()] = (fromUserId.ToString(), sdpOffer);

                    await Clients.User(targetUserId).SendAsync("ReceiveOffer", new CallSignalDto(
                        fromUserId.ToString(),
                        targetUserId,
                        convId.ToString(),
                        "unknown", // Will be determined by context
                        "offer",
                        sdpOffer
                    ));

                    _logger.LogDebug($"Offer sent from {fromUserId} to {targetUserId} in conversation {convId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending offer in conversation {convId}");
                }
            }
        }

        /// <summary>
        /// Send WebRTC answer (SDP)
        /// </summary>
        public async Task SendAnswer(string conversationId, string targetUserId, string sdpAnswer)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var fromUserId) && Guid.TryParse(conversationId, out var convId))
            {
                try
                {
                    await Clients.User(targetUserId).SendAsync("ReceiveAnswer", new CallSignalDto(
                        fromUserId.ToString(),
                        targetUserId,
                        convId.ToString(),
                        "unknown",
                        "answer",
                        sdpAnswer
                    ));

                    _logger.LogDebug($"Answer sent from {fromUserId} to {targetUserId} in conversation {convId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending answer in conversation {convId}");
                }
            }
        }

        /// <summary>
        /// Send ICE candidate
        /// </summary>
        public async Task SendIceCandidate(string conversationId, string targetUserId, string candidate, string? sdpMid, int? sdpMLineIndex)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var fromUserId) && Guid.TryParse(conversationId, out var convId))
            {
                try
                {
                    await Clients.User(targetUserId).SendAsync("ReceiveIceCandidate", new
                    {
                        FromUserId = fromUserId.ToString(),
                        ToUserId = targetUserId,
                        ConversationId = convId.ToString(),
                        Candidate = candidate,
                        SdpMid = sdpMid,
                        SdpMLineIndex = sdpMLineIndex
                    });

                    _logger.LogDebug($"ICE candidate sent from {fromUserId} to {targetUserId} in conversation {convId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending ICE candidate in conversation {convId}");
                }
            }
        }

        /// <summary>
        /// End an ongoing call
        /// </summary>
        public async Task EndCall(string conversationId, string reason = "ended")
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var fromUserId) && Guid.TryParse(conversationId, out var convId))
            {
                try
                {
                    var groupName = $"call_{convId}";

                    // Calculate call duration
                    int? duration = null;
                    if (_activeCalls.TryGetValue(convId.ToString(), out var callInfo))
                    {
                        duration = (int)(DateTime.UtcNow - callInfo.startTime).TotalSeconds;
                        _activeCalls.Remove(convId.ToString());
                    }

                    // Clean up pending offers
                    _pendingOffers.Remove(convId.ToString());

                    // Notify all participants in the call
                    await Clients.Group(groupName).SendAsync("CallEnded", new CallEndedDto(
                        convId.ToString(),
                        fromUserId.ToString(),
                        reason,
                        duration
                    ));

                    _logger.LogInformation($"Call ended in conversation {convId} by user {fromUserId}, duration: {duration}s");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error ending call in conversation {convId} by user {fromUserId}");
                }
            }
        }

        /// <summary>
        /// Toggle audio mute state (notify other participants)
        /// </summary>
        public async Task ToggleAudio(string conversationId, bool isMuted)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId) && Guid.TryParse(conversationId, out var convId))
            {
                try
                {
                    var groupName = $"call_{convId}";
                    await Clients.OthersInGroup(groupName).SendAsync("UserAudioToggled", new
                    {
                        ConversationId = convId.ToString(),
                        UserId = userId.ToString(),
                        IsMuted = isMuted
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error toggling audio in conversation {convId}");
                }
            }
        }

        /// <summary>
        /// Toggle video state (notify other participants)
        /// </summary>
        public async Task ToggleVideo(string conversationId, bool isEnabled)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId) && Guid.TryParse(conversationId, out var convId))
            {
                try
                {
                    var groupName = $"call_{convId}";
                    await Clients.OthersInGroup(groupName).SendAsync("UserVideoToggled", new
                    {
                        ConversationId = convId.ToString(),
                        UserId = userId.ToString(),
                        IsEnabled = isEnabled
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error toggling video in conversation {convId}");
                }
            }
        }

        public override async Task OnConnectedAsync()
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogInformation($"User {userId} connected to call hub");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdStr = Context.UserIdentifier?.ToString();
            if (Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogInformation($"User {userId} disconnected from call hub");

                // Clean up any active calls where this user was a participant
                var callsToEnd = _activeCalls
                    .Where(kvp => kvp.Value.callerId == userId.ToString() || kvp.Value.calleeId == userId.ToString())
                    .ToList();

                foreach (var call in callsToEnd)
                {
                    await EndCall(call.Key, "disconnected");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
