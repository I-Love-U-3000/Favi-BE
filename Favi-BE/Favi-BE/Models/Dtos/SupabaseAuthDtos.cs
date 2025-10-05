using System.Text.Json.Serialization;

namespace Favi_BE.Models.Dtos
{
    public class SupabaseOptions
    {
        public string Url = null!;
        public string ApiKey = null!;
    }

    public record SupabaseAuthResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("expires_at")] long ExpiresAt,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("user")] SupabaseUser User,
        [property: JsonPropertyName("weak_password")] string? WeakPassword
    );

    public record SupabaseUser(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("aud")] string Aud,
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("email_confirmed_at")] DateTime? EmailConfirmedAt,
        [property: JsonPropertyName("phone")] string Phone,
        [property: JsonPropertyName("confirmed_at")] DateTime? ConfirmedAt,
        [property: JsonPropertyName("last_sign_in_at")] DateTime? LastSignInAt,
        [property: JsonPropertyName("app_metadata")] Dictionary<string, object> AppMetadata,
        [property: JsonPropertyName("user_metadata")] Dictionary<string, object> UserMetadata,
        [property: JsonPropertyName("identities")] List<SupabaseIdentity> Identities,
        [property: JsonPropertyName("created_at")] DateTime CreatedAt,
        [property: JsonPropertyName("updated_at")] DateTime UpdatedAt,
        [property: JsonPropertyName("is_anonymous")] bool IsAnonymous
    );

    public record SupabaseIdentity(
        [property: JsonPropertyName("identity_id")] string IdentityId,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("user_id")] string UserId,
        [property: JsonPropertyName("identity_data")] Dictionary<string, object> IdentityData,
        [property: JsonPropertyName("provider")] string Provider,
        [property: JsonPropertyName("last_sign_in_at")] DateTime LastSignInAt,
        [property: JsonPropertyName("created_at")] DateTime CreatedAt,
        [property: JsonPropertyName("updated_at")] DateTime UpdatedAt,
        [property: JsonPropertyName("email")] string Email
    );
}
