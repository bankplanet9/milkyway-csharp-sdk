using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>
    /// Field requirements for one party object. Fields are named by their path within
    /// the object — <c>name.first</c>, <c>name.last</c>, <c>name.full</c>,
    /// <c>date_of_birth</c>, <c>identity_document_type</c>,
    /// <c>identity_document_number</c>, <c>identity_document_country</c>,
    /// <c>country_of_residence</c>, <c>address.country</c>, <c>address.city</c>,
    /// <c>address.street</c>, <c>address.building</c>, <c>address.postal_code</c>,
    /// <c>address.line</c>. A field in neither list is not used by the bank.
    /// </summary>
    public sealed class PartyRequirements
    {
        /// <summary>
        /// Fields a Precheck or Pay must carry. A missing one is refused with
        /// <c>400</c> naming it (e.g. <c>sender_info.date_of_birth</c>).
        /// </summary>
        [JsonPropertyName("required")]
        public IReadOnlyList<string>? Required { get; set; }

        /// <summary>Fields the bank uses when present.</summary>
        [JsonPropertyName("accepted")]
        public IReadOnlyList<string>? Accepted { get; set; }

        /// <summary>
        /// The <see cref="IdentityDocumentType"/> values the bank takes. Null means any;
        /// another kind is refused with <c>400</c> naming <c>identity_document_type</c>.
        /// </summary>
        [JsonPropertyName("identity_document_types")]
        public IReadOnlyList<string>? IdentityDocumentTypes { get; set; }
    }
}
