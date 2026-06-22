namespace Milkyway.Payments.Sdk.Models
{
    /// <summary>
    /// Lifecycle status of a payment transaction, mirroring the integer codes
    /// returned by the Payments API (<c>postcheck</c> / <c>cancel</c>).
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>Accepted, not yet completed. Keep polling.</summary>
        Pending = 0,

        /// <summary>Completed successfully (terminal).</summary>
        Done = 1,

        /// <summary>Cancelled (terminal).</summary>
        Cancelled = 2,

        /// <summary>Failed (terminal). Inspect the accompanying error message.</summary>
        Failed = 3,

        /// <summary>Cancellation requested, awaiting confirmation.</summary>
        CancelPending = 4,

        /// <summary>Timed out / stuck (terminal); requires operator attention.</summary>
        Stuck = 5,
    }

    /// <summary>Helpers over <see cref="TransactionStatus"/>.</summary>
    public static class TransactionStatusExtensions
    {
        /// <summary>
        /// True when the status will not change without external action —
        /// <see cref="TransactionStatus.Done"/>, <see cref="TransactionStatus.Cancelled"/>,
        /// <see cref="TransactionStatus.Failed"/>, or <see cref="TransactionStatus.Stuck"/>.
        /// Polling should stop once a terminal status is reached.
        /// </summary>
        public static bool IsTerminal(this TransactionStatus status)
        {
            switch (status)
            {
                case TransactionStatus.Done:
                case TransactionStatus.Cancelled:
                case TransactionStatus.Failed:
                case TransactionStatus.Stuck:
                    return true;
                default:
                    return false;
            }
        }
    }
}
