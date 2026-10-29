using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>
    /// The canonical identity of a payment party — your customer (<c>sender_info</c>,
    /// the originator) or the beneficiary (<c>recipient_info</c>). The shape is the same
    /// for every recipient bank and service (IVMS101-anchored); the account, card or
    /// phone stays in <c>SenderId</c> / <c>RecipientId</c>. Send what you have — which
    /// fields a service requires is its <see cref="ServiceCapability.PartyProfile"/>.
    /// Null and empty members (including an empty <see cref="Name"/> or
    /// <see cref="Address"/>) are omitted from the wire; the server refuses any key
    /// outside this model.
    /// </summary>
    public sealed class PartyInfo
    {
        /// <summary>
        /// Party kind, always sent. Defaults to <see cref="Models.PartyType.Natural"/>;
        /// <see cref="Models.PartyType.Legal"/> is not accepted by the API yet.
        /// </summary>
        [JsonPropertyName("party_type")]
        public string PartyType { get; set; } = Models.PartyType.Natural;

        /// <summary>The person's name.</summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PartyName? Name { get; set; }

        /// <summary>Date of birth, ISO 8601 <c>YYYY-MM-DD</c> (e.g. <c>1990-04-18</c>).</summary>
        [JsonPropertyName("date_of_birth")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DateOfBirth { get; set; }

        /// <summary>
        /// Identity document kind — one of the <see cref="Models.IdentityDocumentType"/>
        /// values. Required with <see cref="IdentityDocumentNumber"/>.
        /// </summary>
        [JsonPropertyName("identity_document_type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IdentityDocumentType { get; set; }

        /// <summary>Document series and number together, as written in the document.</summary>
        [JsonPropertyName("identity_document_number")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IdentityDocumentNumber { get; set; }

        /// <summary>Issuing country of the document, ISO 3166-1 alpha-2, upper case (e.g. <c>TJ</c>).</summary>
        [JsonPropertyName("identity_document_country")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? IdentityDocumentCountry { get; set; }

        /// <summary>Country of residence, ISO 3166-1 alpha-2, upper case (e.g. <c>AE</c>).</summary>
        [JsonPropertyName("country_of_residence")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CountryOfResidence { get; set; }

        /// <summary>Postal address. When sent, <see cref="PartyAddress.Country"/> is required.</summary>
        [JsonPropertyName("address")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PartyAddress? Address { get; set; }
    }
}
