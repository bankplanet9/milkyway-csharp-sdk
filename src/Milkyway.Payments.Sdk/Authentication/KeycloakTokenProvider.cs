using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk.Exceptions;
using Milkyway.Payments.Sdk.Internal;

namespace Milkyway.Payments.Sdk.Authentication
{
    /// <summary>
    /// Acquires access tokens via the Keycloak client-credentials grant and caches
    /// them until shortly before expiry. Concurrent callers share a single in-flight
    /// acquisition (single-flight) so a burst of requests triggers at most one token
    /// fetch.
    /// </summary>
    public sealed class KeycloakTokenProvider : IAccessTokenProvider
    {
        private readonly HttpClient _httpClient;
        private readonly MilkywayOptions _options;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        private string? _cachedToken;
        private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

        /// <summary>Clock seam for tests. Defaults to wall-clock UTC.</summary>
        internal Func<DateTimeOffset> UtcNow { get; set; } = () => DateTimeOffset.UtcNow;

        /// <param name="httpClient">
        /// A plain client used only to reach the token endpoint. It must NOT route
        /// through the SDK's auth/retry pipeline, to avoid recursion.
        /// </param>
        /// <param name="options">SDK options carrying the token URL and credentials.</param>
        public KeycloakTokenProvider(HttpClient httpClient, MilkywayOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<string> GetAccessTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
        {
            if (!forceRefresh && TryGetCached(out var cached))
                return cached!;

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Re-check under the lock: another caller may have refreshed while we waited.
                if (!forceRefresh && TryGetCached(out var cached2))
                    return cached2!;

                return await AcquireAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        private bool TryGetCached(out string? token)
        {
            if (_cachedToken != null && UtcNow() < _expiresAt - _options.TokenRefreshSkew)
            {
                token = _cachedToken;
                return true;
            }
            token = null;
            return false;
        }

        private async Task<string> AcquireAsync(CancellationToken cancellationToken)
        {
            var form = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
            };
            if (!string.IsNullOrWhiteSpace(_options.Scope))
                form.Add(new KeyValuePair<string, string>("scope", _options.Scope!));

            using (var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl))
            {
                request.Content = new FormUrlEncodedContent(form);

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    throw new MilkywayAuthException("Failed to reach the Keycloak token endpoint.", inner: ex);
                }

                using (response)
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                        throw new MilkywayAuthException(
                            $"Token acquisition failed with status {(int)response.StatusCode}.", body);

                    TokenResponse? parsed;
                    try
                    {
                        parsed = JsonSerializer.Deserialize<TokenResponse>(body, MilkywayJson.Options);
                    }
                    catch (JsonException ex)
                    {
                        throw new MilkywayAuthException("Token endpoint returned a malformed response.", body, ex);
                    }

                    if (parsed?.AccessToken == null || parsed.AccessToken.Length == 0)
                        throw new MilkywayAuthException("Token endpoint response contained no access_token.", body);

                    _cachedToken = parsed.AccessToken;
                    _expiresAt = UtcNow().AddSeconds(parsed.ExpiresIn);
                    return _cachedToken;
                }
            }
        }
    }
}
