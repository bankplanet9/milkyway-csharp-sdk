using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk.Internal;

namespace Milkyway.Payments.Sdk.Authentication
{
    /// <summary>
    /// Delegating handler that attaches a bearer token to every outgoing request.
    /// On a <c>401 Unauthorized</c> it forces a single token refresh and retries
    /// once — covering the case where a cached token was revoked or expired early.
    /// </summary>
    public sealed class ClientCredentialsTokenHandler : DelegatingHandler
    {
        private readonly IAccessTokenProvider _tokenProvider;

        public ClientCredentialsTokenHandler(IAccessTokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Authorize(request, forceRefresh: false, cancellationToken).ConfigureAwait(false);
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            // Token may be stale/revoked: refresh once and replay on a fresh clone
            // (the original instance cannot be sent twice).
            response.Dispose();
            var replay = await HttpRequestMessageCloner.CloneAsync(request).ConfigureAwait(false);
            await Authorize(replay, forceRefresh: true, cancellationToken).ConfigureAwait(false);
            return await base.SendAsync(replay, cancellationToken).ConfigureAwait(false);
        }

        private async Task Authorize(HttpRequestMessage request, bool forceRefresh, CancellationToken cancellationToken)
        {
            var token = await _tokenProvider.GetAccessTokenAsync(forceRefresh, cancellationToken).ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
