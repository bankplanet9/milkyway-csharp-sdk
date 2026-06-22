using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk;
using Milkyway.Payments.Sdk.Authentication;
using Milkyway.Payments.Sdk.Resilience;

namespace Milkyway.Payments.Sdk.Tests
{
    /// <summary>Token provider stub that returns a fixed token and counts refreshes.</summary>
    internal sealed class FakeTokenProvider : IAccessTokenProvider
    {
        public int CallCount;
        public int ForceRefreshCount;
        public string Token = "test-token";

        public Task<string> GetAccessTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref CallCount);
            if (forceRefresh) Interlocked.Increment(ref ForceRefreshCount);
            return Task.FromResult(Token);
        }
    }

    internal static class TestSupport
    {
        public static MilkywayOptions Options(int maxRetries = 3) => new MilkywayOptions
        {
            BaseUrl = "https://milkyway.test",
            TokenUrl = "https://keycloak.test/token",
            ClientId = "bank-alpha",
            ClientSecret = "secret",
            MaxRetries = maxRetries,
            RetryBaseDelay = TimeSpan.FromMilliseconds(1), // keep retry tests fast
            RequestTimeout = TimeSpan.FromSeconds(30),
        };

        /// <summary>
        /// Builds a client over the real resilience + auth pipeline with a stub at
        /// the bottom, so retries and auth behave exactly as in production.
        /// </summary>
        public static MilkywayPaymentsClient BuildClient(
            StubHttpMessageHandler stub,
            out FakeTokenProvider tokenProvider,
            MilkywayOptions? options = null)
        {
            var opts = options ?? Options();
            tokenProvider = new FakeTokenProvider();

            var auth = new ClientCredentialsTokenHandler(tokenProvider) { InnerHandler = stub };
            var resilience = new ResilienceHandler(opts) { InnerHandler = auth };

            var http = new HttpClient(resilience)
            {
                BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/"),
                Timeout = Timeout.InfiniteTimeSpan,
            };
            return new MilkywayPaymentsClient(http);
        }
    }
}
