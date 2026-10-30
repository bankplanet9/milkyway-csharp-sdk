using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Milkyway.Payments.Sdk.Models;

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
            TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { OmitEmptyPartyMembers } },
        };

        // Party objects carry only what the caller filled in: an empty string, line
        // list, name or address would otherwise reach the wire as "" / [] / {}.
        private static void OmitEmptyPartyMembers(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Type != typeof(PartyInfo) && typeInfo.Type != typeof(PartyName) && typeInfo.Type != typeof(PartyAddress))
            {
                return;
            }

            foreach (var property in typeInfo.Properties)
            {
                property.ShouldSerialize = (_, value) => !IsEmpty(value);
            }
        }

        private static bool IsEmpty(object? value)
        {
            switch (value)
            {
                case null:
                    return true;
                case string s:
                    return s.Length == 0;
                case ICollection<string> lines:
                    return lines.Count == 0;
                case PartyName name:
                    return name.IsEmpty;
                case PartyAddress address:
                    return address.IsEmpty;
                default:
                    return false;
            }
        }
    }
}
