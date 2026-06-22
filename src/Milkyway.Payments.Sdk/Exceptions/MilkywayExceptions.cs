using System;
using System.Net;

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

        public MilkywayApiException(HttpStatusCode statusCode, string message, string? responseBody = null, Exception? inner = null)
            : base(message, inner)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }

    /// <summary>HTTP 400 — request validation failed (bad amount, missing field, unresolvable FX rate, …).</summary>
    public sealed class MilkywayValidationException : MilkywayApiException
    {
        public MilkywayValidationException(string message, string? responseBody = null)
            : base(HttpStatusCode.BadRequest, message, responseBody) { }
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

    /// <summary>HTTP 5xx — the API or a downstream recipient service is unavailable.</summary>
    public sealed class MilkywayServiceUnavailableException : MilkywayApiException
    {
        public MilkywayServiceUnavailableException(HttpStatusCode statusCode, string message, string? responseBody = null)
            : base(statusCode, message, responseBody) { }
    }
}
