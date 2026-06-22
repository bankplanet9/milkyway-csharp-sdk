using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>Request to cancel a payment transaction owned by your institution.</summary>
    public sealed class CancelRequest
    {
        [JsonPropertyName("transaction_id")]
        public long TransactionId { get; set; }
    }
}
