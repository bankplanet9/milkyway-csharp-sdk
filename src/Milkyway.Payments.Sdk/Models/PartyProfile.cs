using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>
    /// Which <see cref="PartyInfo"/> fields a service's recipient bank requires and
    /// accepts — the same profile the bank enforces at Precheck and Pay. A party
    /// object the profile does not list needs no fields.
    /// </summary>
    public sealed class PartyProfile
    {
        /// <summary>Requirements for <c>SenderInfo</c> (your customer), when the bank lists any.</summary>
        [JsonPropertyName("sender_info")]
        public PartyRequirements? SenderInfo { get; set; }

        /// <summary>Requirements for <c>RecipientInfo</c> (the beneficiary), when the bank lists any.</summary>
        [JsonPropertyName("recipient_info")]
        public PartyRequirements? RecipientInfo { get; set; }
    }
}
