using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk.Authentication;
using Milkyway.Payments.Sdk.Exceptions;
using Milkyway.Payments.Sdk.Internal;
using Milkyway.Payments.Sdk.Models;
using Milkyway.Payments.Sdk.Resilience;

namespace Milkyway.Payments.Sdk
{
    /// <inheritdoc cref="IMilkywayPaymentsClient" />
    public sealed class MilkywayPaymentsClient : IMilkywayPaymentsClient, IDisposable
    {
        private const string PaymentsPath = "payments/v1";

        private readonly HttpClient _http;
        private readonly bool _ownsHttp;

        /// <summary>
        /// Creates a fully self-contained client: builds the resilience + auth +
        /// Keycloak token pipeline internally. This client owns and disposes its
        /// <see cref="HttpClient"/>.
        /// </summary>
        public MilkywayPaymentsClient(MilkywayOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            options.Validate();

            // Plain client for the token endpoint — must not route through the
            // auth/retry pipeline (would recurse).
            var tokenHttp = new HttpClient();
            var tokenProvider = new KeycloakTokenProvider(tokenHttp, options);

            var primary = new HttpClientHandler();
            var auth = new ClientCredentialsTokenHandler(tokenProvider) { InnerHandler = primary };
            var resilience = new ResilienceHandler(options) { InnerHandler = auth };

            _http = new HttpClient(resilience, disposeHandler: true)
            {
                BaseAddress = NormalizeBaseAddress(options.BaseUrl),
                // Per-attempt timeouts are enforced by the Polly pipeline.
                Timeout = System.Threading.Timeout.InfiniteTimeSpan,
            };
            _ownsHttp = true;
        }

        /// <summary>
        /// Creates a client over a pre-configured <see cref="HttpClient"/> (e.g. one
        /// supplied by <c>IHttpClientFactory</c> with the SDK's handlers attached).
        /// The caller owns the client's lifetime.
        /// </summary>
        public MilkywayPaymentsClient(HttpClient httpClient)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            if (_http.BaseAddress == null)
                throw new ArgumentException("HttpClient.BaseAddress must be set to the Payments API base URL.", nameof(httpClient));
            _ownsHttp = false;
        }

        /// <inheritdoc />
        public async Task<string> HealthcheckAsync(string thirdPartyId, string serviceId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(thirdPartyId)) throw new ArgumentException("thirdPartyId is required.", nameof(thirdPartyId));
            if (string.IsNullOrWhiteSpace(serviceId)) throw new ArgumentException("serviceId is required.", nameof(serviceId));

            var uri = $"{PaymentsPath}/healthcheck?third_party_id={Uri.EscapeDataString(thirdPartyId)}&service_id={Uri.EscapeDataString(serviceId)}";
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            using (var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false))
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw ToException(response.StatusCode, body);
                return body;
            }
        }

        /// <inheritdoc />
        public async Task<PrecheckResult> PrecheckAsync(PrecheckRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            using (var httpRequest = BuildJsonPost($"{PaymentsPath}/precheck", request))
            {
                return await SendForJsonAsync<PrecheckResult>(httpRequest, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<long> PayAsync(PayRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            using (var httpRequest = BuildJsonPost(PaymentsPath, request))
            {
                if (!string.IsNullOrEmpty(idempotencyKey))
                {
                    httpRequest.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
                }
                else
                {
                    // Without an idempotency key a retry could create a duplicate payment.
                    httpRequest.SuppressRetry();
                }

                var result = await SendForJsonAsync<PayResponse>(httpRequest, cancellationToken).ConfigureAwait(false);
                return result.TransactionId;
            }
        }

        /// <inheritdoc />
        public async Task<PostcheckResult> PostcheckAsync(long transactionId, CancellationToken cancellationToken = default)
        {
            var uri = $"{PaymentsPath}/postcheck?transaction_id={transactionId}";
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                return await SendForJsonAsync<PostcheckResult>(request, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<CancelResult> CancelAsync(long transactionId, CancellationToken cancellationToken = default)
        {
            using (var request = BuildJsonPost($"{PaymentsPath}/cancel", new CancelRequest { TransactionId = transactionId }))
            {
                return await SendForJsonAsync<CancelResult>(request, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<PostcheckResult> WaitForCompletionAsync(long transactionId, PollOptions? pollOptions = null, CancellationToken cancellationToken = default)
        {
            var opts = pollOptions ?? new PollOptions();
            var sw = Stopwatch.StartNew();
            var delay = opts.InitialDelay;

            PostcheckResult result = await PostcheckAsync(transactionId, cancellationToken).ConfigureAwait(false);
            while (!result.Status.IsTerminal())
            {
                if (sw.Elapsed + delay > opts.Timeout)
                    return result;

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

                var nextMs = Math.Min(delay.TotalMilliseconds * opts.BackoffMultiplier, opts.MaxDelay.TotalMilliseconds);
                delay = TimeSpan.FromMilliseconds(nextMs);

                result = await PostcheckAsync(transactionId, cancellationToken).ConfigureAwait(false);
            }
            return result;
        }

        private static HttpRequestMessage BuildJsonPost(string uri, object payload)
        {
            var json = JsonSerializer.Serialize(payload, payload.GetType(), MilkywayJson.Options);
            return new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }

        private async Task<T> SendForJsonAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false))
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw ToException(response.StatusCode, body);

                T? parsed;
                try
                {
                    parsed = JsonSerializer.Deserialize<T>(body, MilkywayJson.Options);
                }
                catch (JsonException ex)
                {
                    throw new MilkywayApiException(response.StatusCode, "Failed to parse the API response.", body, ex);
                }

                if (parsed == null)
                    throw new MilkywayApiException(response.StatusCode, "The API returned an empty response.", body);
                return parsed;
            }
        }

        private static MilkywayApiException ToException(HttpStatusCode status, string body)
        {
            var message = ExtractErrorMessage(body) ?? $"Request failed with status {(int)status}.";
            switch (status)
            {
                case HttpStatusCode.BadRequest:
                    return new MilkywayValidationException(message, body);
                case HttpStatusCode.Unauthorized:
                    return new MilkywayAuthException(message, body);
                case HttpStatusCode.PaymentRequired:
                    return new MilkywayExposureBlockedException(message, body);
                case HttpStatusCode.NotFound:
                    return new MilkywayNotFoundException(message, body);
                default:
                    if ((int)status >= 500)
                        return new MilkywayServiceUnavailableException(status, message, body);
                    return new MilkywayApiException(status, message, body);
            }
        }

        private static string? ExtractErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                using (var doc = JsonDocument.Parse(body))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("error", out var err) &&
                        err.ValueKind == JsonValueKind.String)
                    {
                        return err.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                // Non-JSON body (e.g. plain-text 500 from healthcheck) — use it as-is.
                return body.Trim();
            }
            return null;
        }

        private static Uri NormalizeBaseAddress(string baseUrl)
        {
            var trimmed = baseUrl.TrimEnd('/') + "/";
            return new Uri(trimmed, UriKind.Absolute);
        }

        public void Dispose()
        {
            if (_ownsHttp)
                _http.Dispose();
        }
    }
}
