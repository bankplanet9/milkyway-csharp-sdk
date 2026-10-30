using System.Net;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk.Exceptions;
using Milkyway.Payments.Sdk.Models;
using Xunit;

namespace Milkyway.Payments.Sdk.Tests
{
    public class TypedErrorTests
    {
        [Fact]
        public async Task Precheck_422_maps_to_payment_rejected_and_is_not_retried()
        {
            var stub = StubHttpMessageHandler.Always((HttpStatusCode)422, "{\"error\":\"ErrPaymentRejected\"}");
            var client = TestSupport.BuildClient(stub, out _, TestSupport.Options(maxRetries: 3));

            var ex = await Assert.ThrowsAsync<MilkywayPaymentRejectedException>(() => client.PrecheckAsync(NewPrecheck()));

            Assert.Equal((HttpStatusCode)422, ex.StatusCode);
            Assert.Equal("ErrPaymentRejected", ex.ErrorCode);
            Assert.Equal(StatusReasons.PaymentRejected, ex.ErrorCode);
            Assert.Equal(1, stub.CallCount);
        }

        [Fact]
        public async Task Pay_422_with_idempotency_key_is_not_retried()
        {
            var stub = StubHttpMessageHandler.Always((HttpStatusCode)422, "{\"error\":\"ErrPaymentRejected\"}");
            var client = TestSupport.BuildClient(stub, out _, TestSupport.Options(maxRetries: 3));

            var ex = await Assert.ThrowsAsync<MilkywayPaymentRejectedException>(() => client.PayAsync(NewPay(), "idem-key"));

            Assert.IsAssignableFrom<MilkywayApiException>(ex);
            Assert.Equal(1, stub.CallCount);
        }

        [Fact]
        public async Task Pay_502_maps_to_recipient_bank_which_is_a_service_unavailable()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.BadGateway, "{\"error\":\"ErrRecipientBankError\"}");
            var client = TestSupport.BuildClient(stub, out _, TestSupport.Options(maxRetries: 2));

            var ex = await Assert.ThrowsAsync<MilkywayRecipientBankException>(() => client.PayAsync(NewPay(), "idem-key"));

            Assert.IsAssignableFrom<MilkywayServiceUnavailableException>(ex);
            Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
            Assert.Equal("ErrRecipientBankError", ex.ErrorCode);
            Assert.Equal(3, stub.CallCount); // 502 is transient: initial + 2 retries
        }

        [Fact]
        public async Task Existing_service_unavailable_handlers_still_catch_502()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.BadGateway, "{\"error\":\"ErrRecipientBankError\"}");
            var client = TestSupport.BuildClient(stub, out _, TestSupport.Options(maxRetries: 1));

            var ex = await Assert.ThrowsAnyAsync<MilkywayServiceUnavailableException>(() => client.PrecheckAsync(NewPrecheck()));

            Assert.IsType<MilkywayRecipientBankException>(ex);
        }

        [Fact]
        public async Task Pay_502_without_idempotency_key_is_not_retried()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.BadGateway, "{\"error\":\"ErrRecipientBankError\"}");
            var client = TestSupport.BuildClient(stub, out _, TestSupport.Options(maxRetries: 3));

            await Assert.ThrowsAsync<MilkywayRecipientBankException>(() => client.PayAsync(NewPay(), idempotencyKey: null));

            Assert.Equal(1, stub.CallCount);
        }

        [Fact]
        public async Task Cancel_503_exposes_service_and_third_party()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.ServiceUnavailable,
                "{\"error\":\"ErrServiceUnavailable\",\"service_id\":\"wallet\",\"third_party_id\":\"recipient-bank\"}");
            var client = TestSupport.BuildClient(stub, out _, TestSupport.Options(maxRetries: 1));

            var ex = await Assert.ThrowsAsync<MilkywayServiceUnavailableException>(() => client.CancelAsync(4021));

            Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
            Assert.Equal(StatusReasons.ServiceUnavailable, ex.ErrorCode);
            Assert.Equal("wallet", ex.ServiceId);
            Assert.Equal("recipient-bank", ex.ThirdPartyId);
            Assert.Equal(2, stub.CallCount); // retried as before
        }

        [Fact]
        public async Task Cancel_502_maps_to_recipient_bank()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.BadGateway, "{\"error\":\"ErrRecipientBankError\"}");
            var client = TestSupport.BuildClient(stub, out _, TestSupport.Options(maxRetries: 1));

            var ex = await Assert.ThrowsAsync<MilkywayRecipientBankException>(() => client.CancelAsync(4021));

            Assert.Null(ex.ServiceId);
            Assert.Null(ex.ThirdPartyId);
        }

        [Fact]
        public async Task Plain_text_5xx_has_no_error_code()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.InternalServerError, "upstream exploded", "text/plain");
            var client = TestSupport.BuildClient(stub, out _, TestSupport.Options(maxRetries: 1));

            var ex = await Assert.ThrowsAsync<MilkywayServiceUnavailableException>(() => client.PostcheckAsync(1));

            Assert.Null(ex.ErrorCode);
            Assert.Equal("upstream exploded", ex.Message);
        }

        [Theory]
        [InlineData("recipient_id", "ErrRecipientInvalid")]
        [InlineData("currency_credit", "ErrValidationFailed: currency_credit: not accepted by the receiving bank")]
        public async Task Validation_error_exposes_field(string field, string error)
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.BadRequest,
                "{\"error\":\"" + error + "\",\"field\":\"" + field + "\"}");
            var client = TestSupport.BuildClient(stub, out _);

            var ex = await Assert.ThrowsAsync<MilkywayValidationException>(() => client.PrecheckAsync(NewPrecheck()));

            Assert.Equal(field, ex.Field);
            Assert.Equal(error, ex.ErrorCode);
            Assert.Equal(1, stub.CallCount);
        }

        [Fact]
        public async Task Validation_error_without_field_has_null_field()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.BadRequest, "{\"error\":\"quote mismatch\"}");
            var client = TestSupport.BuildClient(stub, out _);

            var ex = await Assert.ThrowsAsync<MilkywayValidationException>(() => client.PrecheckAsync(NewPrecheck()));

            Assert.Null(ex.Field);
        }

        [Theory]
        [InlineData("ErrPaymentRejected", StatusReasons.PaymentRejected)]
        [InlineData("ErrRecipientNotFound", StatusReasons.RecipientNotFound)]
        [InlineData("ErrRecipientInvalid", StatusReasons.RecipientInvalid)]
        [InlineData("ErrServiceUnavailable", StatusReasons.ServiceUnavailable)]
        [InlineData("ErrPaymentCannotBeCanceled", StatusReasons.PaymentCannotBeCanceled)]
        public async Task Postcheck_error_is_a_status_reason_code(string wire, string expected)
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK,
                "{\"transaction_id\":4021,\"status\":3,\"error\":\"" + wire + "\"}");
            var client = TestSupport.BuildClient(stub, out _);

            var result = await client.PostcheckAsync(4021);

            Assert.Equal(TransactionStatus.Failed, result.Status);
            Assert.Equal(expected, result.Error);
        }

        [Fact]
        public async Task Postcheck_unknown_reason_code_still_parses()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK,
                "{\"transaction_id\":4021,\"status\":3,\"error\":\"ErrSomethingNew\"}");
            var client = TestSupport.BuildClient(stub, out _);

            var result = await client.PostcheckAsync(4021);

            Assert.Equal("ErrSomethingNew", result.Error);
        }

        private static PrecheckRequest NewPrecheck() => new PrecheckRequest
        {
            ThirdPartyIdDebit = "bank-beta",
            ServiceId = "card-payout",
            RecipientId = "r-1",
            AmountCredit = 100m,
            CurrencyCredit = "USD",
        };

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
