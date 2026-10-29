using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>A postal address. Text values are at most 256 characters.</summary>
    public sealed class PartyAddress
    {
        /// <summary>Country, ISO 3166-1 alpha-2, upper case. Required when an address is sent.</summary>
        [JsonPropertyName("country")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Country { get; set; }

        [JsonPropertyName("city")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? City { get; set; }

        [JsonPropertyName("street")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Street { get; set; }

        [JsonPropertyName("building")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Building { get; set; }

        [JsonPropertyName("postal_code")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PostalCode { get; set; }

        /// <summary>An address you hold as free text, one line per element (at most 7 lines).</summary>
        [JsonPropertyName("line")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<string>? Line { get; set; }

        internal bool IsEmpty =>
            string.IsNullOrEmpty(Country) && string.IsNullOrEmpty(City) && string.IsNullOrEmpty(Street) &&
            string.IsNullOrEmpty(Building) && string.IsNullOrEmpty(PostalCode) && (Line is null || Line.Count == 0);
    }
}
