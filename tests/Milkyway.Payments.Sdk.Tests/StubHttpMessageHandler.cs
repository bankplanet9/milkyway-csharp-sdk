using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Milkyway.Payments.Sdk.Tests
{
    /// <summary>
    /// Test double for the innermost <see cref="HttpMessageHandler"/>. Records every
    /// request it sees (after the body is buffered) and returns responses from a
    /// caller-supplied responder.
    /// </summary>
    internal sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<int, HttpRequestMessage, string?, HttpResponseMessage> _responder;

        public List<RecordedRequest> Requests { get; } = new List<RecordedRequest>();
        public int CallCount => Requests.Count;

        public StubHttpMessageHandler(Func<int, HttpRequestMessage, string?, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        /// <summary>Convenience: always return the same status + JSON body.</summary>
        public static StubHttpMessageHandler Always(HttpStatusCode status, string body, string contentType = "application/json")
            => new StubHttpMessageHandler((_, __, ___) => Json(status, body, contentType));

        public static HttpResponseMessage Json(HttpStatusCode status, string body, string contentType = "application/json")
            => new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, contentType),
            };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string? body = null;
            if (request.Content != null)
                body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);

            var index = Requests.Count;
            Requests.Add(new RecordedRequest(request.Method, request.RequestUri, body, CloneHeaders(request)));

            return _responder(index, request, body);
        }

        private static Dictionary<string, string> CloneHeaders(HttpRequestMessage request)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in request.Headers)
                headers[h.Key] = string.Join(",", h.Value);
            return headers;
        }
    }

    internal sealed class RecordedRequest
    {
        public HttpMethod Method { get; }
        public Uri? Uri { get; }
        public string? Body { get; }
        public IReadOnlyDictionary<string, string> Headers { get; }

        public RecordedRequest(HttpMethod method, Uri? uri, string? body, IReadOnlyDictionary<string, string> headers)
        {
            Method = method;
            Uri = uri;
            Body = body;
            Headers = headers;
        }
    }
}
