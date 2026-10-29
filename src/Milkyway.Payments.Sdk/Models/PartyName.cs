using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>A natural person's name. Each part is optional; at most 256 characters.</summary>
    public sealed class PartyName
    {
        /// <summary>Given name.</summary>
        [JsonPropertyName("first")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? First { get; set; }

        /// <summary>Family name.</summary>
        [JsonPropertyName("last")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Last { get; set; }

        /// <summary>
        /// The name as written in the document, family name first, with a patronymic
        /// if any (e.g. <c>Petrov Ivan Sergeevich</c>).
        /// </summary>
        [JsonPropertyName("full")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Full { get; set; }

        internal bool IsEmpty =>
            string.IsNullOrEmpty(First) && string.IsNullOrEmpty(Last) && string.IsNullOrEmpty(Full);
    }
}
