using System.Threading;
using System.Threading.Tasks;

namespace Milkyway.Payments.Sdk.Authentication
{
    /// <summary>
    /// Supplies bearer access tokens for API calls. The default implementation
    /// performs a Keycloak client-credentials grant with caching; inject a custom
    /// implementation to integrate an external token source.
    /// </summary>
    public interface IAccessTokenProvider
    {
        /// <summary>
        /// Returns a valid access token, acquiring or refreshing one if necessary.
        /// </summary>
        /// <param name="forceRefresh">
        /// When true, bypasses the cache and acquires a fresh token (used after a 401).
        /// </param>
        /// <param name="cancellationToken">Cancels the token acquisition.</param>
        Task<string> GetAccessTokenAsync(bool forceRefresh, CancellationToken cancellationToken);
    }
}
