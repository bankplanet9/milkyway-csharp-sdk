using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Milkyway.Payments.Sdk.Internal
{
    /// <summary>
    /// Clones an <see cref="HttpRequestMessage"/> so it can be sent more than once.
    /// .NET forbids re-sending the same request instance; retries and the 401 replay
    /// each operate on a fresh clone with the body buffered into memory.
    /// </summary>
    internal static class HttpRequestMessageCloner
    {
        public static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage source)
        {
            var clone = new HttpRequestMessage(source.Method, source.RequestUri)
            {
                Version = source.Version,
            };

            if (source.Content != null)
            {
                var bytes = await source.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                var newContent = new ByteArrayContent(bytes);
                foreach (var header in source.Content.Headers)
                    newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                clone.Content = newContent;
            }

            foreach (var header in source.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

#if NET8_0_OR_GREATER
            foreach (var option in source.Options)
                ((System.Collections.Generic.IDictionary<string, object?>)clone.Options)[option.Key] = option.Value;
#else
            foreach (var prop in source.Properties)
                clone.Properties[prop.Key] = prop.Value;
#endif

            return clone;
        }
    }
}
