using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk.Authentication;
using Milkyway.Payments.Sdk.Exceptions;
using Xunit;

namespace Milkyway.Payments.Sdk.Tests
{
    public class TokenProviderTests
    {
        private static KeycloakTokenProvider Build(StubHttpMessageHandler stub, out DateTimeOffset[] now)
        {
            var clock = new DateTimeOffset[] { DateTimeOffset.UnixEpoch };
            now = clock;
            var http = new HttpClient(stub);
            var provider = new KeycloakTokenProvider(http, TestSupport.Options())
            {
                UtcNow = () => clock[0],
            };
            return provider;
        }

        private static StubHttpMessageHandler TokenStub(int expiresIn = 3600)
            => StubHttpMessageHandler.Always(HttpStatusCode.OK,
                "{\"access_token\":\"tok-" + expiresIn + "\",\"token_type\":\"Bearer\",\"expires_in\":" + expiresIn + "}");

        [Fact]
        public async Task Caches_token_across_calls()
        {
            var stub = TokenStub();
            var provider = Build(stub, out _);

            var a = await provider.GetAccessTokenAsync(false, CancellationToken.None);
            var b = await provider.GetAccessTokenAsync(false, CancellationToken.None);

            Assert.Equal(a, b);
            Assert.Equal(1, stub.CallCount); // acquired once, served from cache afterward
        }

        [Fact]
        public async Task Refreshes_after_expiry_minus_skew()
        {
            var stub = TokenStub(expiresIn: 3600);
            var provider = Build(stub, out var now);

            await provider.GetAccessTokenAsync(false, CancellationToken.None);

            // Advance past (expiry - 30s skew): expiresAt=3600s, threshold=3570s.
            now[0] = now[0].AddSeconds(3580);
            await provider.GetAccessTokenAsync(false, CancellationToken.None);

            Assert.Equal(2, stub.CallCount);
        }

        [Fact]
        public async Task Force_refresh_bypasses_cache()
        {
            var stub = TokenStub();
            var provider = Build(stub, out _);

            await provider.GetAccessTokenAsync(false, CancellationToken.None);
            await provider.GetAccessTokenAsync(true, CancellationToken.None);

            Assert.Equal(2, stub.CallCount);
        }

        [Fact]
        public async Task Concurrent_callers_trigger_single_acquisition()
        {
            // The first in-flight acquisition is held until all callers have queued,
            // proving they coalesce onto a single token fetch (single-flight).
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var stub = new StubHttpMessageHandler((_, __, ___) =>
            {
                release.Task.GetAwaiter().GetResult(); // block this attempt until released
                return StubHttpMessageHandler.Json(HttpStatusCode.OK,
                    "{\"access_token\":\"tok\",\"expires_in\":3600}");
            });
            var provider = Build(stub, out _);

            // Start on the thread pool so a synchronous block inside the stub can never
            // stall the test thread before it releases the gate.
            var tasks = new Task<string>[5];
            for (var i = 0; i < tasks.Length; i++)
                tasks[i] = Task.Run(() => provider.GetAccessTokenAsync(false, CancellationToken.None));

            await Task.Delay(50); // let callers queue on the provider's single-flight gate
            release.SetResult(true);
            await Task.WhenAll(tasks);

            Assert.Equal(1, stub.CallCount); // single-flight: only one acquisition
        }

        [Fact]
        public async Task Failed_acquisition_throws_auth_exception()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.BadRequest, "{\"error\":\"invalid_client\"}");
            var provider = Build(stub, out _);

            await Assert.ThrowsAsync<MilkywayAuthException>(() =>
                provider.GetAccessTokenAsync(false, CancellationToken.None));
        }
    }
}
