using System.Collections.Generic;
using System.Text.Json;
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
        /// Lists the recipient banks you can pay and the services each offers. Pass
        /// <paramref name="country"/> (ISO 3166-1 alpha-2, case-insensitive) to filter
        /// by the recipient bank's country, or leave it null to list every destination.
        /// The per-service JSON Schema is omitted here — fetch it with
        /// <see cref="ServiceSchemaAsync"/>.
        /// </summary>
        Task<IReadOnlyList<Destination>> DiscoveryAsync(string? country = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the raw Draft-2020 JSON Schema that a recipient bank's service
        /// validates its payment <c>data</c> (non-identity extras) against, as a
        /// <see cref="JsonElement"/>. Identity requirements are not here — they are the
        /// service's <see cref="ServiceCapability.PartyProfile"/>.
        /// </summary>
        Task<JsonElement> ServiceSchemaAsync(string thirdPartyId, string serviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies that a recipient bank's service is reachable. Returns the
        /// service's plain-text status message.
        /// </summary>
        Task<string> HealthcheckAsync(string thirdPartyId, string serviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a quote (rate, debit amount, commission) for a prospective
        /// payment without creating it. FX markup is applied here.
        /// </summary>
        /// <exception cref="Exceptions.MilkywayValidationException">400 — invalid request; <see cref="Exceptions.MilkywayValidationException.Field"/> names the field at fault (e.g. <c>recipient_id</c> when the receiving bank reports the recipient invalid).</exception>
        /// <exception cref="Exceptions.MilkywayExposureBlockedException">402 — the payment would breach a block-action exposure limit.</exception>
        /// <exception cref="Exceptions.MilkywayPaymentRejectedException">422 — the receiving bank refused the payment; not retryable, nothing created.</exception>
        /// <exception cref="Exceptions.MilkywayRecipientBankException">502 — the receiving bank's connector failed unclassifiably; nothing created.</exception>
        /// <exception cref="Exceptions.MilkywayServiceUnavailableException">Other 5xx — the recipient service is unreachable or quoted no FX rate.</exception>
        Task<PrecheckResult> PrecheckAsync(PrecheckRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initiates a payment and returns the new transaction id. Supply
        /// <paramref name="idempotencyKey"/> to make the call safely retryable —
        /// without it, the SDK does not auto-retry this call to avoid duplicates.
        /// </summary>
        /// <exception cref="Exceptions.MilkywayValidationException">400 — invalid request; <see cref="Exceptions.MilkywayValidationException.Field"/> names the field at fault (e.g. <c>recipient_id</c> when the receiving bank reports the recipient invalid).</exception>
        /// <exception cref="Exceptions.MilkywayExposureBlockedException">402 — the payment would breach a block-action exposure limit.</exception>
        /// <exception cref="Exceptions.MilkywayPaymentRejectedException">422 — the receiving bank refused the payment; not retryable, nothing created.</exception>
        /// <exception cref="Exceptions.MilkywayRecipientBankException">502 — the receiving bank's connector failed unclassifiably; nothing created.</exception>
        /// <exception cref="Exceptions.MilkywayServiceUnavailableException">Other 5xx — the recipient service is unreachable or quoted no FX rate.</exception>
        Task<long> PayAsync(PayRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = default);

        /// <summary>Returns the current status of one of your transactions.</summary>
        Task<PostcheckResult> PostcheckAsync(long transactionId, CancellationToken cancellationToken = default);

        /// <summary>Requests cancellation of one of your transactions.</summary>
        /// <exception cref="Exceptions.MilkywayNotFoundException">404 — not found or not owned by your institution.</exception>
        /// <exception cref="Exceptions.MilkywayApiException">403 (<c>ErrPaymentCannotBeCanceled</c>) — the payment can no longer be cancelled; its status is unchanged.</exception>
        /// <exception cref="Exceptions.MilkywayRecipientBankException">502 — the receiving bank's connector failed; the payment is unchanged.</exception>
        /// <exception cref="Exceptions.MilkywayServiceUnavailableException">503 — the receiving bank cannot be reached (<see cref="Exceptions.MilkywayServiceUnavailableException.ServiceId"/> / <see cref="Exceptions.MilkywayServiceUnavailableException.ThirdPartyId"/> name it); the payment is unchanged.</exception>
        Task<CancelResult> CancelAsync(long transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Polls <see cref="PostcheckAsync"/> with exponential backoff until the
        /// transaction reaches a terminal status (done, cancelled, failed, stuck)
        /// or the poll options are exhausted.
        /// </summary>
        Task<PostcheckResult> WaitForCompletionAsync(long transactionId, PollOptions? pollOptions = null, CancellationToken cancellationToken = default);
    }
}
