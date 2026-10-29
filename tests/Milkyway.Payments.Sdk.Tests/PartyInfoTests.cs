using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk.Models;
using Xunit;

namespace Milkyway.Payments.Sdk.Tests
{
    public class PartyInfoTests
    {
        private static JsonElement Body(StubHttpMessageHandler stub)
            => JsonDocument.Parse(stub.Requests[0].Body!).RootElement;

        [Fact]
        public async Task Precheck_serializes_every_party_field_under_its_wire_key()
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
                SenderInfo = new PartyInfo
                {
                    PartyType = PartyType.Natural,
                    Name = new PartyName { First = "Ivan", Last = "Petrov", Full = "Petrov Ivan Sergeevich" },
                    DateOfBirth = "1990-04-18",
                    IdentityDocumentType = IdentityDocumentType.Passport,
                    IdentityDocumentNumber = "AB1234567",
                    IdentityDocumentCountry = "TJ",
                    CountryOfResidence = "AE",
                    Address = new PartyAddress
                    {
                        Country = "AE",
                        City = "Dubai",
                        Street = "Sheikh Zayed Rd",
                        Building = "12",
                        PostalCode = "00000",
                        Line = new List<string> { "Sheikh Zayed Rd 12, Dubai" },
                    },
                },
            });

            Assert.Equal(
                "{\"party_type\":\"natural\"," +
                "\"name\":{\"first\":\"Ivan\",\"last\":\"Petrov\",\"full\":\"Petrov Ivan Sergeevich\"}," +
                "\"date_of_birth\":\"1990-04-18\"," +
                "\"identity_document_type\":\"passport\"," +
                "\"identity_document_number\":\"AB1234567\"," +
                "\"identity_document_country\":\"TJ\"," +
                "\"country_of_residence\":\"AE\"," +
                "\"address\":{\"country\":\"AE\",\"city\":\"Dubai\",\"street\":\"Sheikh Zayed Rd\"," +
                "\"building\":\"12\",\"postal_code\":\"00000\",\"line\":[\"Sheikh Zayed Rd 12, Dubai\"]}}",
                Body(stub).GetProperty("sender_info").GetRawText());
            Assert.False(Body(stub).TryGetProperty("recipient_info", out _));
        }

        [Fact]
        public async Task Pay_omits_null_and_empty_party_fields_and_empty_sub_objects()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK, "{\"transaction_id\":1}");
            var client = TestSupport.BuildClient(stub, out _);

            await client.PayAsync(new PayRequest
            {
                ThirdPartyIdDebit = "bank-beta",
                ServiceId = "card-payout",
                SenderId = "s-1",
                RecipientId = "r-1",
                AmountCredit = 100m,
                CurrencyCredit = "USD",
                SenderInfo = new PartyInfo
                {
                    Name = new PartyName { First = "Ivan", Last = "Petrov", Full = "" },
                    IdentityDocumentType = IdentityDocumentType.NationalId,
                    IdentityDocumentNumber = "A-1",
                    CountryOfResidence = "",
                    Address = new PartyAddress { Line = new List<string>() },
                },
                RecipientInfo = new PartyInfo
                {
                    Name = new PartyName(),
                    Address = new PartyAddress { Country = "TJ", City = "Dushanbe", Street = "" },
                },
            }, "k");

            var body = Body(stub);
            Assert.Equal(
                "{\"party_type\":\"natural\",\"name\":{\"first\":\"Ivan\",\"last\":\"Petrov\"}," +
                "\"identity_document_type\":\"national_id\",\"identity_document_number\":\"A-1\"}",
                body.GetProperty("sender_info").GetRawText());
            Assert.Equal(
                "{\"party_type\":\"natural\",\"address\":{\"country\":\"TJ\",\"city\":\"Dushanbe\"}}",
                body.GetProperty("recipient_info").GetRawText());
            Assert.False(body.TryGetProperty("data", out _));
        }

        [Fact]
        public async Task Data_is_sent_alongside_party_info()
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
                SenderInfo = new PartyInfo { Name = new PartyName { Full = "Petrov Ivan" } },
                Data = new Dictionary<string, object?> { ["test_outcome"] = "done" },
            });

            var body = Body(stub);
            Assert.Equal("{\"party_type\":\"natural\",\"name\":{\"full\":\"Petrov Ivan\"}}", body.GetProperty("sender_info").GetRawText());
            Assert.Equal("{\"test_outcome\":\"done\"}", body.GetProperty("data").GetRawText());
        }

        [Fact]
        public void Identity_document_type_constants_match_the_wire_vocabulary()
        {
            Assert.Equal(
                new[] { "passport", "national_id", "driver_license", "residence_permit", "tax_id", "social_security", "other" },
                new[]
                {
                    IdentityDocumentType.Passport, IdentityDocumentType.NationalId, IdentityDocumentType.DriverLicense,
                    IdentityDocumentType.ResidencePermit, IdentityDocumentType.TaxId, IdentityDocumentType.SocialSecurity,
                    IdentityDocumentType.Other,
                });
            Assert.Equal("natural", PartyType.Natural);
            Assert.Equal("legal", PartyType.Legal);
        }
    }
}
