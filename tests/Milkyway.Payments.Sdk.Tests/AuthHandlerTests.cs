using System.Net;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk.Exceptions;
using Xunit;

namespace Milkyway.Payments.Sdk.Tests
{
    public class AuthHandlerTests
    {
        [Fact]
        public async Task On_401_forces_token_refresh_and_replays_once()
        {
            // First call 401 (stale token), second call succeeds after refresh.
            var stub = new StubHttpMessageHandler((i, _, __) => i == 0
                ? StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, "{\"error\":\"expired\"}")
                : StubHttpMessageHandler.Json(HttpStatusCode.OK, "{\"transaction_id\":4021,\"status\":1}"));
            var client = TestSupport.BuildClient(stub, out var tokenProvider);

            var result = await client.PostcheckAsync(4021);

            Assert.Equal(Models.TransactionStatus.Done, result.Status);
            Assert.Equal(2, stub.CallCount);
            Assert.Equal(1, tokenProvider.ForceRefreshCount); // refreshed exactly once
        }

        [Fact]
        public async Task Persistent_401_surfaces_as_auth_exception_after_single_replay()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.Unauthorized, "{\"error\":\"invalid\"}");
            var client = TestSupport.BuildClient(stub, out var tokenProvider);

            await Assert.ThrowsAsync<MilkywayAuthException>(() => client.PostcheckAsync(4021));

            Assert.Equal(2, stub.CallCount);          // original + one replay, no infinite loop
            Assert.Equal(1, tokenProvider.ForceRefreshCount);
        }
    }
}
