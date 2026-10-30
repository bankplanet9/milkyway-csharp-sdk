namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>
    /// Wire values of <see cref="PartyInfo.IdentityDocumentType"/> — a closed vocabulary,
    /// one-to-one with the IVMS101 national identifier types.
    /// </summary>
    public static class IdentityDocumentType
    {
        /// <summary>Passport (IVMS101 <c>CCPT</c>).</summary>
        public const string Passport = "passport";

        /// <summary>National identity card (IVMS101 <c>IDCD</c>).</summary>
        public const string NationalId = "national_id";

        /// <summary>Driver's license (IVMS101 <c>DRLC</c>).</summary>
        public const string DriverLicense = "driver_license";

        /// <summary>Residence permit (IVMS101 <c>ARNU</c>).</summary>
        public const string ResidencePermit = "residence_permit";

        /// <summary>Tax identification number (IVMS101 <c>TXID</c>).</summary>
        public const string TaxId = "tax_id";

        /// <summary>Social security number (IVMS101 <c>SOCS</c>).</summary>
        public const string SocialSecurity = "social_security";

        /// <summary>Any other document (IVMS101 <c>MISC</c>).</summary>
        public const string Other = "other";
    }
}
