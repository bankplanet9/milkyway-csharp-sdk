namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>
    /// Codes the API puts in <see cref="PostcheckResult.Error"/>: the receiving bank's reason
    /// for a terminal outcome. The field stays a string so a code added later still
    /// deserializes; compare it against these constants and treat any other value as an
    /// unknown reason.
    /// </summary>
    public static class StatusReasons
    {
        /// <summary>The receiving bank declined the payment (also any reason it gave without a code).</summary>
        public const string PaymentRejected = "ErrPaymentRejected";

        /// <summary>The receiving bank could not find the recipient.</summary>
        public const string RecipientNotFound = "ErrRecipientNotFound";

        /// <summary>The receiving bank reports the recipient as invalid.</summary>
        public const string RecipientInvalid = "ErrRecipientInvalid";

        /// <summary>The receiving bank's service was unavailable.</summary>
        public const string ServiceUnavailable = "ErrServiceUnavailable";

        /// <summary>The receiving bank could not cancel the payment.</summary>
        public const string PaymentCannotBeCanceled = "ErrPaymentCannotBeCanceled";
    }
}
