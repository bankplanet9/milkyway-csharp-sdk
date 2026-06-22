using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk.Internal;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Milkyway.Payments.Sdk.Resilience
{
    /// <summary>
    /// Delegating handler that applies a Polly resilience pipeline: a per-attempt
    /// timeout wrapped by exponential-backoff-with-jitter retries on transient
    /// failures (HTTP 5xx, 408, and network/timeout exceptions).
    ///
    /// Deterministic outcomes (400, 401, 402, 404) are never retried. A request
    /// explicitly opted out via <see cref="RetryPolicyMarker.SuppressRetry"/> — a
    /// non-idempotent <c>pay</c> without an Idempotency-Key — bypasses retries
    /// entirely to avoid duplicate side effects.
    /// </summary>
    public sealed class ResilienceHandler : DelegatingHandler
    {
        private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
        private readonly ResiliencePipeline<HttpResponseMessage> _timeoutOnlyPipeline;

        public ResilienceHandler(MilkywayOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = args =>
                    {
                        if (args.Outcome.Exception is HttpRequestException)
                            return PredicateResult.True();
                        if (args.Outcome.Exception is TimeoutRejectedException)
                            return PredicateResult.True();

                        var response = args.Outcome.Result;
                        if (response != null && IsTransient(response.StatusCode))
                            return PredicateResult.True();

                        return PredicateResult.False();
                    },
                    MaxRetryAttempts = options.MaxRetries,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = options.RetryBaseDelay,
                })
                .AddTimeout(options.RequestTimeout)
                .Build();

            // For non-retryable requests we still want the per-attempt timeout.
            _timeoutOnlyPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddTimeout(options.RequestTimeout)
                .Build();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var pipeline = request.IsRetrySuppressed() ? _timeoutOnlyPipeline : _retryPipeline;

            return await pipeline.ExecuteAsync(async token =>
            {
                // Each attempt sends a fresh clone — a request instance is single-use.
                var attempt = await HttpRequestMessageCloner.CloneAsync(request).ConfigureAwait(false);
                return await base.SendAsync(attempt, token).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static bool IsTransient(HttpStatusCode status)
        {
            return (int)status >= 500 || status == HttpStatusCode.RequestTimeout;
        }
    }
}
