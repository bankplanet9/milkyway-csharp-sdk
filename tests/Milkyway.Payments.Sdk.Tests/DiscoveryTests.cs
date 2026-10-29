using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk.Exceptions;
using Xunit;

namespace Milkyway.Payments.Sdk.Tests
{
    public class DiscoveryTests
    {
        private const string CatalogBody =
            "{\"destinations\":[{" +
            "\"third_party_id\":\"bank-beta\"," +
            "\"partner_name\":\"Bank Beta\"," +
            "\"country\":\"TJ\"," +
            "\"services\":[{" +
            "\"service_id\":\"card-payout\"," +
            "\"payout_method\":\"card\"," +
            "\"recipient\":\"card_number\"," +
            "\"payout_currencies\":[\"TJS\",\"USD\"]}]}]}";

        [Fact]
        public async Task Discovery_without_country_omits_query_and_unwraps_destinations()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK, CatalogBody);
            var client = TestSupport.BuildClient(stub, out _);

            var destinations = await client.DiscoveryAsync();

            var dest = Assert.Single(destinations);
            Assert.Equal("bank-beta", dest.ThirdPartyId);
            Assert.Equal("Bank Beta", dest.PartnerName);
            Assert.Equal("TJ", dest.Country);
            var service = Assert.Single(dest.Services);
            Assert.Equal("card-payout", service.ServiceId);
            Assert.Equal("card", service.PayoutMethod);
            Assert.Equal("card_number", service.Recipient);
            Assert.Equal(new[] { "TJS", "USD" }, service.PayoutCurrencies);

            var req = stub.Requests[0];
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.EndsWith("/payments/v1/discovery", req.Uri!.AbsoluteUri);
            Assert.Equal("", req.Uri!.Query);
            Assert.Equal("Bearer test-token", req.Headers["Authorization"]);
        }

        [Fact]
        public async Task Discovery_decodes_party_profile_and_tolerates_its_absence()
        {
            const string body =
                "{\"destinations\":[{\"third_party_id\":\"bank-beta\",\"services\":[" +
                "{\"service_id\":\"card-payout\",\"party_profile\":{" +
                "\"sender_info\":{\"required\":[\"name.first\",\"name.last\",\"date_of_birth\"," +
                "\"identity_document_type\",\"identity_document_number\"]," +
                "\"accepted\":[\"name.full\"],\"identity_document_types\":[\"passport\"]}," +
                "\"recipient_info\":{\"required\":[\"name.full\"]}}}," +
                "{\"service_id\":\"wallet\"}]}]}";
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK, body);
            var client = TestSupport.BuildClient(stub, out _);

            var destinations = await client.DiscoveryAsync();

            var services = Assert.Single(destinations).Services;
            var profile = services[0].PartyProfile;
            Assert.NotNull(profile);
            Assert.Equal(
                new[] { "name.first", "name.last", "date_of_birth", "identity_document_type", "identity_document_number" },
                profile!.SenderInfo!.Required);
            Assert.Equal(new[] { "name.full" }, profile.SenderInfo.Accepted);
            Assert.Equal(new[] { Models.IdentityDocumentType.Passport }, profile.SenderInfo.IdentityDocumentTypes);
            Assert.Equal(new[] { "name.full" }, profile.RecipientInfo!.Required);
            Assert.Null(profile.RecipientInfo.Accepted);
            Assert.Null(profile.RecipientInfo.IdentityDocumentTypes);

            Assert.Null(services[1].PartyProfile);
        }

        [Fact]
        public async Task Discovery_with_country_adds_query()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK, "{\"destinations\":[]}");
            var client = TestSupport.BuildClient(stub, out _);

            var destinations = await client.DiscoveryAsync("TJ");

            Assert.Empty(destinations);
            var req = stub.Requests[0];
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Contains("country=TJ", req.Uri!.Query);
        }

        [Fact]
        public async Task Discovery_unauthorized_maps_to_auth_exception()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.Unauthorized, "{\"error\":\"unauthorized\"}");
            var client = TestSupport.BuildClient(stub, out _);

            await Assert.ThrowsAsync<MilkywayAuthException>(() => client.DiscoveryAsync());
        }

        [Fact]
        public async Task ServiceSchema_returns_raw_schema_and_encodes_path()
        {
            const string schema =
                "{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\"," +
                "\"type\":\"object\"," +
                "\"required\":[\"passport\"]," +
                "\"properties\":{\"passport\":{\"type\":\"string\"}}}";
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK, schema);
            var client = TestSupport.BuildClient(stub, out _);

            JsonElement result = await client.ServiceSchemaAsync("bank-beta", "card-payout");

            Assert.Equal(JsonValueKind.Object, result.ValueKind);
            Assert.Equal("object", result.GetProperty("type").GetString());
            Assert.Equal("string", result.GetProperty("properties").GetProperty("passport").GetProperty("type").GetString());

            var req = stub.Requests[0];
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.EndsWith("/payments/v1/discovery/bank-beta/card-payout/schema", req.Uri!.AbsoluteUri);
        }

        [Fact]
        public async Task ServiceSchema_not_found_maps_to_404_exception()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.NotFound, "{\"error\":\"not found\"}");
            var client = TestSupport.BuildClient(stub, out _);

            await Assert.ThrowsAsync<MilkywayNotFoundException>(() => client.ServiceSchemaAsync("bank-beta", "no-such-service"));
        }
    }
}
