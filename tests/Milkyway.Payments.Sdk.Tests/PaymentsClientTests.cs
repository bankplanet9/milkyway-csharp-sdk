using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk.Exceptions;
using Milkyway.Payments.Sdk.Models;
using Xunit;

namespace Milkyway.Payments.Sdk.Tests
{
    public class PaymentsClientTests
    {
        [Fact]
        public async Task Precheck_parses_decimals_losslessly_and_attaches_bearer()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK,
                "{\"third_party_id\":\"bank-beta\",\"amount_credit\":100.00,\"amount_debit\":1267500.00,\"rate\":12675.00,\"commission\":1.50}");
            var client = TestSupport.BuildClient(stub, out _);

            var result = await client.PrecheckAsync(new PrecheckRequest
            {
                ThirdPartyIdDebit = "bank-beta",
                ServiceId = "card-payout",
                RecipientId = "recipient-9999",
                AmountCredit = 100.00m,
                CurrencyCredit = "USD",
            });

            Assert.Equal(12675.00m, result.Rate);
            Assert.Equal(1267500.00m, result.AmountDebit);
            Assert.Equal(1.50m, result.Commission);

            var req = stub.Requests[0];
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.EndsWith("/payments/v1/precheck", req.Uri!.AbsoluteUri);
            Assert.Equal("Bearer test-token", req.Headers["Authorization"]);
        }

        [Fact]
        public async Task Precheck_serializes_snake_case_and_omits_null_data()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK, "{}");
            var client = TestSupport.BuildClient(stub, out _);

            await client.PrecheckAsync(new PrecheckRequest
            {
                ThirdPartyIdDebit = "bank-beta",
                ServiceId = "card-payout",
                RecipientId = "r-1",
                AmountCredit = 50m,
                CurrencyCredit = "USD",
            });

            var body = stub.Requests[0].Body!;
            Assert.Contains("\"third_party_id_debit\":\"bank-beta\"", body);
            Assert.Contains("\"amount_credit\":50", body);
            Assert.DoesNotContain("\"data\"", body); // null data is omitted
        }

        [Fact]
        public async Task Pay_returns_transaction_id_and_forwards_idempotency_key()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK, "{\"transaction_id\":4021}");
            var client = TestSupport.BuildClient(stub, out _);

            var id = await client.PayAsync(new PayRequest
            {
                ThirdPartyIdDebit = "bank-beta",
                ServiceId = "card-payout",
                SenderId = "s-1",
                RecipientId = "r-1",
                AmountCredit = 100m,
                CurrencyCredit = "USD",
            }, idempotencyKey: "key-123");

            Assert.Equal(4021, id);
            Assert.Equal("key-123", stub.Requests[0].Headers["Idempotency-Key"]);
        }

        [Theory]
        [InlineData(0, TransactionStatus.Pending)]
        [InlineData(1, TransactionStatus.Done)]
        [InlineData(3, TransactionStatus.Failed)]
        [InlineData(5, TransactionStatus.Stuck)]
        public async Task Postcheck_maps_integer_status_to_enum(int code, TransactionStatus expected)
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK,
                "{\"transaction_id\":4021,\"status\":" + code + "}");
            var client = TestSupport.BuildClient(stub, out _);

            var result = await client.PostcheckAsync(4021);

            Assert.Equal(expected, result.Status);
            Assert.Equal(4021, result.TransactionId);
        }

        [Fact]
        public async Task Healthcheck_returns_plaintext_body()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK, "Service is available", "text/plain");
            var client = TestSupport.BuildClient(stub, out _);

            var body = await client.HealthcheckAsync("bank-beta", "card-payout");

            Assert.Equal("Service is available", body);
            Assert.Contains("third_party_id=bank-beta", stub.Requests[0].Uri!.Query);
            Assert.Contains("service_id=card-payout", stub.Requests[0].Uri!.Query);
        }

        [Fact]
        public async Task Validation_error_maps_to_typed_exception_with_message()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.BadRequest,
                "{\"error\":\"amount_credit must be > 0\"}");
            var client = TestSupport.BuildClient(stub, out _);

            var ex = await Assert.ThrowsAsync<MilkywayValidationException>(() =>
                client.PrecheckAsync(new PrecheckRequest { ThirdPartyIdDebit = "b", ServiceId = "s", RecipientId = "r", AmountCredit = 0m, CurrencyCredit = "USD" }));

            Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
            Assert.Equal("amount_credit must be > 0", ex.Message);
        }

        [Fact]
        public async Task Exposure_block_maps_to_402_exception()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.PaymentRequired,
                "{\"error\":\"exposure limit exceeded\"}");
            var client = TestSupport.BuildClient(stub, out _);

            var ex = await Assert.ThrowsAsync<MilkywayExposureBlockedException>(() =>
                client.PayAsync(new PayRequest { ThirdPartyIdDebit = "b", ServiceId = "s", SenderId = "x", RecipientId = "r", AmountCredit = 1m, CurrencyCredit = "USD" }, "k"));

            Assert.Equal(HttpStatusCode.PaymentRequired, ex.StatusCode);
        }

        [Fact]
        public async Task Postcheck_not_owned_maps_to_404_exception()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.NotFound, "{\"error\":\"not found\"}");
            var client = TestSupport.BuildClient(stub, out _);

            await Assert.ThrowsAsync<MilkywayNotFoundException>(() => client.PostcheckAsync(999));
        }

        [Fact]
        public async Task Cancel_returns_resulting_status()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK, "{\"status\":2}");
            var client = TestSupport.BuildClient(stub, out _);

            var result = await client.CancelAsync(4021);

            Assert.Equal(TransactionStatus.Cancelled, result.Status);
            Assert.Contains("\"transaction_id\":4021", stub.Requests[0].Body);
        }
    }
}
