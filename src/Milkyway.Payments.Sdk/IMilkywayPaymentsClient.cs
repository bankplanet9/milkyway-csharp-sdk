using System.Threading;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk.Models;

namespace Milkyway.Payments.Sdk
{
    /// <summary>
    /// Client for the MilkyWay Payments API (<c>/payments/v1</c>). All calls are
    /// authenticated automatically with a cached Keycloak access token; transient
    /// failures are retried (see <see cref="MilkywayOptions"/>).
    /// </summary>
    public interface IMilkywayPaymentsClient
    {
        /// <summary>
        /// Verifies that a recipient bank's service is reachable. Returns the
        /// service's plain-text status message.
        /// </summary>
        Task<string> HealthcheckAsync(string thirdPartyId, string serviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a quote (rate, debit amount, commission) for a prospective
        /// payment without creating it. FX markup is applied here.
        /// </summary>
        Task<PrecheckResult> PrecheckAsync(PrecheckRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initiates a payment and returns the new transaction id. Supply
        /// <paramref name="idempotencyKey"/> to make the call safely retryable —
        /// without it, the SDK does not auto-retry this call to avoid duplicates.
        /// </summary>
        Task<long> PayAsync(PayRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = default);

        /// <summary>Returns the current status of one of your transactions.</summary>
        Task<PostcheckResult> PostcheckAsync(long transactionId, CancellationToken cancellationToken = default);

        /// <summary>Requests cancellation of one of your transactions.</summary>
        Task<CancelResult> CancelAsync(long transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Polls <see cref="PostcheckAsync"/> with exponential backoff until the
        /// transaction reaches a terminal status (done, cancelled, failed, stuck)
        /// or the poll options are exhausted.
        /// </summary>
        Task<PostcheckResult> WaitForCompletionAsync(long transactionId, PollOptions? pollOptions = null, CancellationToken cancellationToken = default);
    }
}
