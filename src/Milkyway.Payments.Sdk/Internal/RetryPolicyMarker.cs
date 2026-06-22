using System.Net.Http;

namespace Milkyway.Payments.Sdk.Internal
{
    /// <summary>
    /// Per-request flag used to opt a request <em>out</em> of automatic retries.
    /// Set on a non-idempotent <c>pay</c> call that has no Idempotency-Key, where a
    /// retry could create a duplicate payment. Stored on the request so the Polly
    /// resilience handler can read it back without coupling to call-site state.
    /// </summary>
    internal static class RetryPolicyMarker
    {
        private const string Key = "Milkyway.NoRetry";

        public static void SuppressRetry(this HttpRequestMessage request)
        {
#if NET8_0_OR_GREATER
            request.Options.Set(new HttpRequestOptionsKey<bool>(Key), true);
#else
            request.Properties[Key] = true;
#endif
        }

        public static bool IsRetrySuppressed(this HttpRequestMessage request)
        {
#if NET8_0_OR_GREATER
            return request.Options.TryGetValue(new HttpRequestOptionsKey<bool>(Key), out var v) && v;
#else
            return request.Properties.TryGetValue(Key, out var v) && v is true;
#endif
        }
    }
}
