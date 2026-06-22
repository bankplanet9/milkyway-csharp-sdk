using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>Current status of a payment transaction.</summary>
    public sealed class PostcheckResult
    {
        [JsonPropertyName("transaction_id")]
        public long TransactionId { get; set; }

        /// <summary>Lifecycle status of the transaction.</summary>
        [JsonPropertyName("status")]
        public TransactionStatus Status { get; set; }

        /// <summary>Failure detail; present only when the payment failed.</summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
