using System;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Milkyway.Payments.Sdk.Authentication;
using Milkyway.Payments.Sdk.Resilience;

namespace Milkyway.Payments.Sdk.DependencyInjection
{
    /// <summary>DI registration for <see cref="IMilkywayPaymentsClient"/>.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IMilkywayPaymentsClient"/> as a typed
        /// <c>IHttpClientFactory</c> client with the SDK's resilience and auth
        /// handlers wired in. The token-endpoint client is kept separate so token
        /// acquisition never routes through the retry/auth pipeline.
        /// </summary>
        public static IServiceCollection AddMilkywayPayments(this IServiceCollection services, Action<MilkywayOptions> configure)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            services.AddSingleton(sp =>
            {
                var options = new MilkywayOptions();
                configure(options);
                options.Validate();
                return options;
            });

            // Plain client used only for the Keycloak token endpoint.
            services.AddHttpClient("Milkyway.Token");

            services.AddSingleton<IAccessTokenProvider>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var options = sp.GetRequiredService<MilkywayOptions>();
                return new KeycloakTokenProvider(factory.CreateClient("Milkyway.Token"), options);
            });

            services.AddTransient(sp => new ResilienceHandler(sp.GetRequiredService<MilkywayOptions>()));
            services.AddTransient(sp => new ClientCredentialsTokenHandler(sp.GetRequiredService<IAccessTokenProvider>()));

            services
                .AddHttpClient<IMilkywayPaymentsClient, MilkywayPaymentsClient>((sp, client) =>
                {
                    var options = sp.GetRequiredService<MilkywayOptions>();
                    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
                    client.Timeout = Timeout.InfiniteTimeSpan; // per-attempt timeout is enforced by Polly
                })
                // First added handler is outermost: retry wraps auth.
                .AddHttpMessageHandler<ResilienceHandler>()
                .AddHttpMessageHandler<ClientCredentialsTokenHandler>();

            return services;
        }
    }
}
