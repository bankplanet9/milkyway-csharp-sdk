using System;
using System.Net;
using System.Threading.Tasks;
using Milkyway.Payments.Sdk;
using Milkyway.Payments.Sdk.Models;
using Xunit;

namespace Milkyway.Payments.Sdk.Tests
{
    public class WaitForCompletionTests
    {
        private static readonly PollOptions FastPoll = new PollOptions
        {
            InitialDelay = TimeSpan.FromMilliseconds(1),
            MaxDelay = TimeSpan.FromMilliseconds(5),
            BackoffMultiplier = 2.0,
            Timeout = TimeSpan.FromSeconds(10),
        };

        [Fact]
        public async Task Polls_until_terminal_status()
        {
            // pending, pending, done
            var stub = new StubHttpMessageHandler((i, _, __) =>
            {
                var status = i < 2 ? 0 : 1;
                return StubHttpMessageHandler.Json(HttpStatusCode.OK,
                    "{\"transaction_id\":4021,\"status\":" + status + "}");
            });
            var client = TestSupport.BuildClient(stub, out _);

            var result = await client.WaitForCompletionAsync(4021, FastPoll);

            Assert.Equal(TransactionStatus.Done, result.Status);
            Assert.Equal(3, stub.CallCount);
        }

        [Fact]
        public async Task Returns_last_status_when_poll_budget_exhausted()
        {
            var stub = StubHttpMessageHandler.Always(HttpStatusCode.OK, "{\"transaction_id\":4021,\"status\":0}");
            var client = TestSupport.BuildClient(stub, out _);

            var shortBudget = new PollOptions
            {
                InitialDelay = TimeSpan.FromMilliseconds(5),
                MaxDelay = TimeSpan.FromMilliseconds(5),
                Timeout = TimeSpan.FromMilliseconds(20),
            };

            var result = await client.WaitForCompletionAsync(4021, shortBudget);

            Assert.Equal(TransactionStatus.Pending, result.Status); // non-terminal, budget exhausted
        }
    }
}
