namespace Favi_BE.API.Models.Dtos
{
    /// <summary>
    /// Base signal for WebRTC communication
    /// </summary>
    public record CallSignalDto(
        string FromUserId,
        string ToUserId,
        string ConversationId,
        string CallType, // "audio" or "video"
        string SignalType, // "offer", "answer", "ice-candidate"
        string Data
    );

    /// <summary>
    /// ICE candidate data
    /// </summary>
    public record IceCandidateDto(
        string Candidate,
        string? SdpMid,
        int? SdpMLineIndex
    );

    /// <summary>
    /// Incoming call request
    /// </summary>
    public record IncomingCallRequestDto(
        string ConversationId,
        string CallerId,
        string CallerUsername,
        string? CallerDisplayName,
        string? CallerAvatarUrl,
        string CallType // "audio" or "video"
    );

    /// <summary>
    /// Call response (accept/reject)
    /// </summary>
    public record CallResponseDto(
        string ConversationId,
        string Response, // "accept" or "reject"
        string? Reason
    );

    /// <summary>
    /// Call ended notification
    /// </summary>
    public record CallEndedDto(
        string ConversationId,
        string EndedByUserId,
        string Reason, // "ended", "rejected", "timeout", "error"
        int? DurationSeconds
    );

    /// <summary>
    /// User joined call notification
    /// </summary>
    public record CallUserJoinedDto(
        string ConversationId,
        string UserId,
        string Username,
        string? DisplayName
    );

    /// <summary>
    /// User left call notification
    /// </summary>
    public record CallUserLeftDto(
        string ConversationId,
        string UserId,
        string Reason
    );
}
