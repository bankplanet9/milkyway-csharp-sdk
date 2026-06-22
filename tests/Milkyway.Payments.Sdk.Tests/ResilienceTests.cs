using System.Net;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk.Exceptions;
using Milkyway.Payments.Sdk.Models;
using Xunit;

namespace Milkyway.Payments.Sdk.Tests
{
    public class ResilienceTests
    {
        [Fact]
        public async Task Retries_transient_5xx_then_succeeds()
        {
            // Two 503s, then a 200.
            var stub = new StubHttpMessageHandler((i, _, __) => i < 2
                ? StubHttpMessageHandler.Json(HttpStatusCode.ServiceUnavailable, "{\"error\":\"down\"}")
                : StubHttpMessageHandler.Json(HttpStatusCode.OK, "{\"transaction_id\":4021,\"status\":1}"));
            var client = TestSupport.BuildClient(stub, out _, TestSupport.Options(maxRetries: 3));

            var result = await client.PostcheckAsync(4021);

            Assert.Equal(TransactionStatus.Done, result.Status);
            Assert.Equal(3, stub.CallCount); // 2 failures + 1 success
        }

        [Fact]
        public async Task Gives_up_after_max_retries_and_throws()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.ServiceUnavailable, "{\"error\":\"down\"}");
            var client = TestSupport.BuildClient(stub, out _, TestSupport.Options(maxRetries: 2));

            await Assert.ThrowsAsync<MilkywayServiceUnavailableException>(() => client.PostcheckAsync(4021));

            Assert.Equal(3, stub.CallCount); // initial + 2 retries
        }

        [Fact]
        public async Task Does_not_retry_deterministic_402()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.PaymentRequired, "{\"error\":\"blocked\"}");
            var client = TestSupport.BuildClient(stub, out _, TestSupport.Options(maxRetries: 3));

            await Assert.ThrowsAsync<MilkywayExposureBlockedException>(() =>
                client.PayAsync(NewPay(), "idem-key"));

            Assert.Equal(1, stub.CallCount); // no retries on 402
        }

        [Fact]
        public async Task Pay_without_idempotency_key_is_not_retried_on_5xx()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.ServiceUnavailable, "{\"error\":\"down\"}");
            var client = TestSupport.BuildClient(stub, out _, TestSupport.Options(maxRetries: 3));

            await Assert.ThrowsAsync<MilkywayServiceUnavailableException>(() =>
                client.PayAsync(NewPay(), idempotencyKey: null));

            Assert.Equal(1, stub.CallCount); // suppressed: no duplicate-payment risk
        }

        [Fact]
        public async Task Pay_with_idempotency_key_is_retried_on_5xx()
        {
            var stub = new StubHttpMessageHandler((i, _, __) => i < 1
                ? StubHttpMessageHandler.Json(HttpStatusCode.ServiceUnavailable, "{\"error\":\"down\"}")
                : StubHttpMessageHandler.Json(HttpStatusCode.OK, "{\"transaction_id\":7}"));
            var client = TestSupport.BuildClient(stub, out _, TestSupport.Options(maxRetries: 3));

            var id = await client.PayAsync(NewPay(), idempotencyKey: "idem-key");

            Assert.Equal(7, id);
            Assert.Equal(2, stub.CallCount);
        }

        private static PayRequest NewPay() => new PayRequest
        {
            ThirdPartyIdDebit = "bank-beta",
            ServiceId = "card-payout",
            SenderId = "s-1",
            RecipientId = "r-1",
            AmountCredit = 100m,
            CurrencyCredit = "USD",
        };
    }
}
