using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>Wire response of a successful <c>pay</c> call.</summary>
    internal sealed class PayResponse
    {
        [JsonPropertyName("transaction_id")]
        public long TransactionId { get; set; }
    }
}
