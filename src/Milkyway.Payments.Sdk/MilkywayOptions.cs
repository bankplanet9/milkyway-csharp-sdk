using System;

namespace Milkyway.Payments.Sdk
{
    /// <summary>
    /// Configuration for <see cref="MilkywayPaymentsClient"/>: API endpoint, Keycloak
    /// client-credentials, and resilience tuning. Construct directly or bind from
    /// configuration (e.g. <c>appsettings.json</c>) when using the DI extension.
    /// </summary>
    public sealed class MilkywayOptions
    {
        /// <summary>Base URL of the Payments API, e.g. <c>https://milkyway.stage.planet9.ae</c>.</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Keycloak token endpoint for the client-credentials grant, e.g.
        /// <c>https://keycloak.ac8o.planet9.ae/realms/planet9-stage/protocol/openid-connect/token</c>.
        /// </summary>
        public string TokenUrl { get; set; } = string.Empty;

        /// <summary>Keycloak client id issued to your institution (becomes the <c>azp</c> claim).</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>Keycloak client secret issued to your institution.</summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>Optional OAuth scope to request. Empty by default.</summary>
        public string? Scope { get; set; }

        /// <summary>
        /// How long before a token's stated expiry it is considered stale and
        /// refreshed. Guards against clock skew and in-flight latency. Default 30s.
        /// </summary>
        public TimeSpan TokenRefreshSkew { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Per-attempt request timeout. Default 30s.</summary>
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Maximum automatic retries for transient failures (5xx, 408, network). Default 3.</summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>Base delay for exponential backoff between retries. Default 500ms.</summary>
        public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
                throw new ArgumentException("BaseUrl is required.", nameof(BaseUrl));
            if (string.IsNullOrWhiteSpace(TokenUrl))
                throw new ArgumentException("TokenUrl is required.", nameof(TokenUrl));
            if (string.IsNullOrWhiteSpace(ClientId))
                throw new ArgumentException("ClientId is required.", nameof(ClientId));
            if (string.IsNullOrWhiteSpace(ClientSecret))
                throw new ArgumentException("ClientSecret is required.", nameof(ClientSecret));
            if (MaxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(MaxRetries), "MaxRetries cannot be negative.");
        }
    }
}
