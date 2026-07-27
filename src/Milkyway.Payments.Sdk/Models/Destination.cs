using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>
    /// A recipient bank you can pay, together with the services it offers. Returned
    /// by the discovery catalog (<see cref="IMilkywayPaymentsClient.DiscoveryAsync"/>).
    /// </summary>
    public sealed class Destination
    {
        /// <summary>Recipient bank id — use as <c>ThirdPartyIdDebit</c> when paying.</summary>
        [JsonPropertyName("third_party_id")]
        public string? ThirdPartyId { get; set; }

        /// <summary>Human-readable partner name, when advertised.</summary>
        [JsonPropertyName("partner_name")]
        public string? PartnerName { get; set; }

        /// <summary>Country of the recipient bank (ISO 3166-1 alpha-2), when advertised.</summary>
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        /// <summary>Services the recipient bank offers.</summary>
        [JsonPropertyName("services")]
        public IReadOnlyList<ServiceCapability> Services { get; set; } = new List<ServiceCapability>();
    }
}
