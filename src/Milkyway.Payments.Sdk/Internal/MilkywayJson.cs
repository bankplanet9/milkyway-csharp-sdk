using System.Text.Json;

namespace Milkyway.Payments.Sdk.Internal
{
    /// <summary>
    /// Shared <see cref="JsonSerializerOptions"/> for the SDK. Enums are intentionally
    /// serialised as numbers (the API encodes transaction status as an integer), and
    /// money fields bind to <see cref="decimal"/> losslessly because they arrive as
    /// raw JSON numbers.
    /// </summary>
    internal static class MilkywayJson
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // explicit [JsonPropertyName] on every member
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict,
        };
    }
}
