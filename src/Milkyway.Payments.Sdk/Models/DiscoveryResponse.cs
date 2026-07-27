using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>Wire response of the discovery catalog endpoint.</summary>
    internal sealed class DiscoveryResponse
    {
        [JsonPropertyName("destinations")]
        public List<Destination> Destinations { get; set; } = new List<Destination>();
    }
}
