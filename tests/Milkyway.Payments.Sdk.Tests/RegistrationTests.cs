using System;
using Microsoft.Extensions.DependencyInjection;
using Milkyway.Payments.Sdk;
using Milkyway.Payments.Sdk.DependencyInjection;
using Xunit;

namespace Milkyway.Payments.Sdk.Tests
{
    public class RegistrationTests
    {
        [Fact]
        public void AddMilkywayPayments_resolves_a_usable_client()
        {
            var services = new ServiceCollection();
            services.AddMilkywayPayments(o =>
            {
                o.BaseUrl = "https://milkyway.test";
                o.TokenUrl = "https://keycloak.test/token";
                o.ClientId = "bank-alpha";
                o.ClientSecret = "secret";
            });

            using var provider = services.BuildServiceProvider();
            var client = provider.GetRequiredService<IMilkywayPaymentsClient>();

            Assert.NotNull(client);
        }

        [Fact]
        public void Invalid_options_fail_fast_on_resolution()
        {
            var services = new ServiceCollection();
            services.AddMilkywayPayments(o => { /* missing required fields */ });

            using var provider = services.BuildServiceProvider();

            Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<IMilkywayPaymentsClient>());
        }

        [Theory]
        [InlineData("", "t", "c", "s")]
        [InlineData("b", "", "c", "s")]
        [InlineData("b", "t", "", "s")]
        [InlineData("b", "t", "c", "")]
        public void Options_validate_requires_all_credentials(string baseUrl, string tokenUrl, string clientId, string secret)
        {
            var options = new MilkywayOptions
            {
                BaseUrl = baseUrl,
                TokenUrl = tokenUrl,
                ClientId = clientId,
                ClientSecret = secret,
            };

            Assert.Throws<ArgumentException>(() => new MilkywayPaymentsClient(options));
        }
    }
}
