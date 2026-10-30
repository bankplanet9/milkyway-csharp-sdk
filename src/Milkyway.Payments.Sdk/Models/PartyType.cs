namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>Wire values of <see cref="PartyInfo.PartyType"/>.</summary>
    public static class PartyType
    {
        /// <summary>A natural person.</summary>
        public const string Natural = "natural";

        /// <summary>A legal entity. Part of the contract, but not accepted by the API yet.</summary>
        public const string Legal = "legal";
    }
}
