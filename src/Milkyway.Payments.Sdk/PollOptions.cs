using System;

namespace Milkyway.Payments.Sdk
{
    /// <summary>Controls <see cref="IMilkywayPaymentsClient.WaitForCompletionAsync"/> polling.</summary>
    public sealed class PollOptions
    {
        /// <summary>Delay before the first poll. Default 1s.</summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>Maximum delay between polls (backoff is capped here). Default 30s.</summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Multiplier applied to the delay after each poll. Default 2.0.</summary>
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Overall budget for polling. When exceeded, the last observed status is
        /// returned even if non-terminal. Default 5 minutes.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}
