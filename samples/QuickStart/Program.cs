using Milkyway.Payments.Sdk;
using Milkyway.Payments.Sdk.Exceptions;
using Milkyway.Payments.Sdk.Models;

// A runnable end-to-end walk-through of the payments flow. Supply credentials via
// environment variables:
//   MILKYWAY_BASE_URL, MILKYWAY_TOKEN_URL, MILKYWAY_CLIENT_ID, MILKYWAY_CLIENT_SECRET

var options = new MilkywayOptions
{
    BaseUrl      = Env("MILKYWAY_BASE_URL", "https://milkyway.stage.planet9.ae"),
    TokenUrl     = Env("MILKYWAY_TOKEN_URL", "https://keycloak.ac8o.planet9.ae/realms/planet9-stage/protocol/openid-connect/token"),
    ClientId     = Env("MILKYWAY_CLIENT_ID", ""),
    ClientSecret = Env("MILKYWAY_CLIENT_SECRET", ""),
};

if (string.IsNullOrEmpty(options.ClientId) || string.IsNullOrEmpty(options.ClientSecret))
{
    Console.Error.WriteLine("Set MILKYWAY_CLIENT_ID and MILKYWAY_CLIENT_SECRET to run this sample.");
    return 1;
}

const string recipientBank = "bank-beta";
const string serviceId = "card-payout";

using var client = new MilkywayPaymentsClient(options);

try
{
    Console.WriteLine($"Healthcheck {recipientBank}/{serviceId}...");
    Console.WriteLine("  " + await client.HealthcheckAsync(recipientBank, serviceId));

    Console.WriteLine("Precheck...");
    var quote = await client.PrecheckAsync(new PrecheckRequest
    {
        ThirdPartyIdDebit = recipientBank,
        ServiceId         = serviceId,
        RecipientId       = "recipient-9999",
        AmountCredit      = 100.00m,
        CurrencyCredit    = "USD",
    });
    Console.WriteLine($"  rate={quote.Rate}, debit={quote.AmountDebit} {quote.CurrencyDebit}, commission={quote.Commission}");

    Console.WriteLine("Pay...");
    var transactionId = await client.PayAsync(new PayRequest
    {
        ThirdPartyIdDebit = recipientBank,
        ServiceId         = serviceId,
        SenderId          = "sender-0001",
        RecipientId       = "recipient-9999",
        AmountCredit      = 100.00m,
        CurrencyCredit    = "USD",
        Data              = new Dictionary<string, object?> { ["passport"] = "AA1234567" },
    }, idempotencyKey: Guid.NewGuid().ToString());
    Console.WriteLine($"  transaction_id={transactionId}");

    Console.WriteLine("Waiting for completion...");
    var result = await client.WaitForCompletionAsync(transactionId);
    Console.WriteLine($"  final status={result.Status}" + (result.Error is null ? "" : $" ({result.Error})"));

    return result.Status == TransactionStatus.Done ? 0 : 2;
}
catch (MilkywayExposureBlockedException ex)
{
    Console.Error.WriteLine($"Blocked by exposure limit: {ex.Message}");
    return 3;
}
catch (MilkywayApiException ex)
{
    Console.Error.WriteLine($"API error {(int)ex.StatusCode}: {ex.Message}");
    return 4;
}

static string Env(string key, string fallback)
    => Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : fallback;
