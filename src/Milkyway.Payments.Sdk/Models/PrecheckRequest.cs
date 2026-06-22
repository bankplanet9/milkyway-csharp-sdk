using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>
    /// Request for a payment quote (rate + commission) without creating a payment.
    /// The credit party (your institution) is resolved from the access token's
    /// <c>azp</c> claim and must not be sent here.
    /// </summary>
    public sealed class PrecheckRequest
    {
        /// <summary>Recipient bank id.</summary>
        [JsonPropertyName("third_party_id_debit")]
        public string ThirdPartyIdDebit { get; set; } = string.Empty;

        /// <summary>Service id (e.g. <c>card-payout</c>).</summary>
        [JsonPropertyName("service_id")]
        public string ServiceId { get; set; } = string.Empty;

        /// <summary>Recipient identifier within the chosen service.</summary>
        [JsonPropertyName("recipient_id")]
        public string RecipientId { get; set; } = string.Empty;

        /// <summary>Amount in the credit currency (must be &gt; 0).</summary>
        [JsonPropertyName("amount_credit")]
        public decimal AmountCredit { get; set; }

        /// <summary>Credit currency (ISO-4217), e.g. <c>USD</c>.</summary>
        [JsonPropertyName("currency_credit")]
        public string CurrencyCredit { get; set; } = string.Empty;

        /// <summary>
        /// Service-specific payload, validated server-side against the service's
        /// JSON Schema. Required keys depend on <see cref="ServiceId"/> and the
        /// recipient bank — see the Услуги registry. Omitted from the wire when null.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, object?>? Data { get; set; }
    }
}
