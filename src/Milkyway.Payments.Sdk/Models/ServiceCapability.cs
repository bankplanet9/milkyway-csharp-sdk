using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>
    /// A single payout service a recipient bank offers, as advertised by the
    /// discovery catalog. The service's JSON Schema is not included here — fetch it
    /// on demand with <see cref="IMilkywayPaymentsClient.ServiceSchemaAsync"/>.
    /// </summary>
    public sealed class ServiceCapability
    {
        /// <summary>Service id (e.g. <c>card-payout</c>).</summary>
        [JsonPropertyName("service_id")]
        public string? ServiceId { get; set; }

        /// <summary>Payout method the service settles over (e.g. <c>card</c>, <c>account</c>).</summary>
        [JsonPropertyName("payout_method")]
        public string? PayoutMethod { get; set; }

        /// <summary>Recipient identifier kind the service expects, when advertised.</summary>
        [JsonPropertyName("recipient")]
        public string? Recipient { get; set; }

        /// <summary>Currencies the service can pay out in (ISO-4217), when advertised.</summary>
        [JsonPropertyName("payout_currencies")]
        public IReadOnlyList<string>? PayoutCurrencies { get; set; }
    }
}
