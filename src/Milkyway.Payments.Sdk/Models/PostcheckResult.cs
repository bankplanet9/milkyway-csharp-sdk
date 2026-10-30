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

        /// <summary>
        /// The code of the receiving bank's reason for a terminal outcome, when it gave one:
        /// one of the <see cref="StatusReasons"/> constants (never the bank's own text).
        /// <c>null</c> when no reason was given. Kept a string so codes added later still parse.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
