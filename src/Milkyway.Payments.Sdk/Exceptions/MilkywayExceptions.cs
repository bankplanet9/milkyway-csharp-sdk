using System;
using System.Net;
using System.Text.Json;

namespace Milkyway.Payments.Sdk.Exceptions
{
    /// <summary>
    /// Base exception for all errors returned by the MilkyWay Payments API.
    /// Carries the HTTP status code and the server's <c>error</c> message when present.
    /// </summary>
    public class MilkywayApiException : Exception
    {
        /// <summary>HTTP status code returned by the API (0 if the request never completed).</summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>Raw response body, useful for diagnostics when no structured error was parsed.</summary>
        public string? ResponseBody { get; }

        /// <summary>
        /// The machine-readable code leading the server's error message (e.g.
        /// <c>ErrPaymentRejected</c>, <c>ErrRecipientBankError</c>, or <c>ErrValidationFailed</c>
        /// for <c>"ErrValidationFailed: amount_credit: …"</c>), or <c>null</c> when the message
        /// does not start with an <c>Err…</c> identifier. The full text stays in
        /// <see cref="Exception.Message"/>.
        /// </summary>
        public string? ErrorCode { get; }

        public MilkywayApiException(HttpStatusCode statusCode, string message, string? responseBody = null, Exception? inner = null)
            : base(message, inner)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
            ErrorCode = ErrorBody.LeadingCode(message);
        }
    }

    /// <summary>
    /// HTTP 400 — request validation failed (bad amount, missing field, unresolvable FX rate,
    /// a recipient the receiving bank reports as invalid, …).
    /// </summary>
    public sealed class MilkywayValidationException : MilkywayApiException
    {
        /// <summary>
        /// The JSON name of the request field at fault on a field-level validation error
        /// (for example <c>amount_credit</c>, <c>recipient_id</c>, <c>currency_credit</c> or
        /// <c>sender_info.date_of_birth</c>); <c>null</c> when the error is not tied to one field.
        /// </summary>
        public string? Field { get; }

        public MilkywayValidationException(string message, string? responseBody = null)
            : base(HttpStatusCode.BadRequest, message, responseBody)
        {
            Field = ErrorBody.ReadString(responseBody, "field");
        }
    }

    /// <summary>HTTP 401 — missing, malformed, or invalid access token (incl. failed token acquisition).</summary>
    public sealed class MilkywayAuthException : MilkywayApiException
    {
        public MilkywayAuthException(string message, string? responseBody = null, Exception? inner = null)
            : base(HttpStatusCode.Unauthorized, message, responseBody, inner) { }
    }

    /// <summary>
    /// HTTP 402 — rejected because the payment would breach a block-action exposure
    /// limit (effective limit = configured limit + funded deposit balance).
    /// </summary>
    public sealed class MilkywayExposureBlockedException : MilkywayApiException
    {
        public MilkywayExposureBlockedException(string message, string? responseBody = null)
            : base(HttpStatusCode.PaymentRequired, message, responseBody) { }
    }

    /// <summary>HTTP 404 — transaction not found, or not owned by your institution.</summary>
    public sealed class MilkywayNotFoundException : MilkywayApiException
    {
        public MilkywayNotFoundException(string message, string? responseBody = null)
            : base(HttpStatusCode.NotFound, message, responseBody) { }
    }

    /// <summary>
    /// HTTP 422 — the receiving bank refused the payment on its merits (for example a limit
    /// or a blocked account); <see cref="MilkywayApiException.ErrorCode"/> is
    /// <c>ErrPaymentRejected</c>. The request was well formed, but repeating it will not
    /// change the answer, so it is never retried. Nothing was created or debited; the bank's
    /// own reason is not disclosed.
    /// </summary>
    public sealed class MilkywayPaymentRejectedException : MilkywayApiException
    {
        public MilkywayPaymentRejectedException(string message, string? responseBody = null)
            : base((HttpStatusCode)422, message, responseBody) { }
    }

    /// <summary>
    /// HTTP 5xx — the API or a downstream recipient service is unavailable (retried
    /// automatically first). Nothing was created or debited.
    /// </summary>
    public class MilkywayServiceUnavailableException : MilkywayApiException
    {
        /// <summary>
        /// The downstream service that failed, when the body names it (a 503 from the
        /// recipient institution carries <c>service_id</c>); otherwise <c>null</c>.
        /// </summary>
        public string? ServiceId { get; }

        /// <summary>
        /// The downstream institution that failed, when the body names it (a 503 from the
        /// recipient institution carries <c>third_party_id</c>); otherwise <c>null</c>.
        /// </summary>
        public string? ThirdPartyId { get; }

        public MilkywayServiceUnavailableException(HttpStatusCode statusCode, string message, string? responseBody = null)
            : base(statusCode, message, responseBody)
        {
            ServiceId = ErrorBody.ReadString(responseBody, "service_id");
            ThirdPartyId = ErrorBody.ReadString(responseBody, "third_party_id");
        }
    }

    /// <summary>
    /// HTTP 502 — the receiving bank's connector failed in a way MilkyWay could not classify;
    /// <see cref="MilkywayApiException.ErrorCode"/> is <c>ErrRecipientBankError</c>. Nothing
    /// was created or debited. Retried automatically first like any 5xx; afterwards you may
    /// retry later or route the payment through another service. Derives from
    /// <see cref="MilkywayServiceUnavailableException"/>, so existing handlers for that type
    /// still catch it.
    /// </summary>
    public sealed class MilkywayRecipientBankException : MilkywayServiceUnavailableException
    {
        public MilkywayRecipientBankException(string message, string? responseBody = null)
            : base(HttpStatusCode.BadGateway, message, responseBody) { }
    }

    internal static class ErrorBody
    {
        internal static string? LeadingCode(string? message)
        {
            if (string.IsNullOrEmpty(message)) return null;
            var colon = message!.IndexOf(':');
            var code = (colon >= 0 ? message.Substring(0, colon) : message).Trim();
            if (!code.StartsWith("Err", StringComparison.Ordinal)) return null;
            foreach (var c in code)
            {
                var identifierChar = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
                if (!identifierChar) return null;
            }
            return code;
        }

        internal static string? ReadString(string? body, string property)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                using (var doc = JsonDocument.Parse(body!))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty(property, out var value) &&
                        value.ValueKind == JsonValueKind.String)
                    {
                        return value.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                // Non-JSON body (e.g. a plain-text 5xx from a proxy): no structured fields.
            }
            return null;
        }
    }
}
