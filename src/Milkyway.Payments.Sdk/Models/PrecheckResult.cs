using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>
    /// Quote for a prospective payment. The FX markup is already applied and is
    /// locked onto the transaction if you proceed to <c>pay</c>.
    /// </summary>
    public sealed class PrecheckResult
    {
        /// <summary>Recipient bank id (echoed).</summary>
        [JsonPropertyName("third_party_id")]
        public string? ThirdPartyId { get; set; }

        [JsonPropertyName("service_id")]
        public string? ServiceId { get; set; }

        [JsonPropertyName("recipient_id")]
        public string? RecipientId { get; set; }

        [JsonPropertyName("amount_credit")]
        public decimal AmountCredit { get; set; }

        [JsonPropertyName("currency_credit")]
        public string? CurrencyCredit { get; set; }

        /// <summary>Amount the recipient bank is debited, in the debit currency.</summary>
        [JsonPropertyName("amount_debit")]
        public decimal AmountDebit { get; set; }

        [JsonPropertyName("currency_debit")]
        public string? CurrencyDebit { get; set; }

        /// <summary>Quoted exchange rate: <c>1 CREDIT = rate DEBIT</c>.</summary>
        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }

        /// <summary>Commission charged, in the credit currency.</summary>
        [JsonPropertyName("commission")]
        public decimal Commission { get; set; }
    }
}
