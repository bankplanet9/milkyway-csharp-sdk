using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Authentication
{
    /// <summary>Subset of the OAuth2 token endpoint response we consume.</summary>
    internal sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        /// <summary>Lifetime of the access token in seconds.</summary>
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
