using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>Result of a cancellation request: the resulting transaction status.</summary>
    public sealed class CancelResult
    {
        [JsonPropertyName("status")]
        public TransactionStatus Status { get; set; }
    }
}
