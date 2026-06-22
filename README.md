# MilkyWay Payments SDK for .NET

Official C# client for the **MilkyWay Payments API** (`/payments/v1`) — the
partner-facing API that banks use to initiate, quote, track, and cancel
cross-bank payments.

Batteries included:

- **Keycloak client-credentials auth** with in-memory token caching and automatic
  refresh (and a one-shot refresh-and-replay on `401`).
- **Retries** via [Polly](https://github.com/App-vNext/Polly): exponential backoff
  with jitter on transient failures (5xx, 408, network), with deterministic errors
  (400/401/402/404) never retried.
- **Typed models & exceptions** — money is `decimal`, status is an `enum`, and each
  HTTP error maps to a specific exception type.
- **`IHttpClientFactory` / DI** integration, plus a plain `new MilkywayPaymentsClient(options)`
  for non-DI apps.
- Multi-targets **`netstandard2.0`** (works on .NET Framework 4.6.1+, Mono, Xamarin)
  and **`net8.0`**.

## Install

```bash
dotnet add package Milkyway.Payments.Sdk
```

## Quick start

```csharp
using Milkyway.Payments.Sdk;
using Milkyway.Payments.Sdk.Models;

using var client = new MilkywayPaymentsClient(new MilkywayOptions
{
    BaseUrl     = "https://milkyway.stage.planet9.ae",
    TokenUrl    = "https://keycloak.ac8o.planet9.ae/realms/planet9-stage/protocol/openid-connect/token",
    ClientId    = "your-client-id",      // issued to your institution
    ClientSecret = "your-client-secret",
});

// 1. Is the recipient bank's service online?
await client.HealthcheckAsync("bank-beta", "card-payout");

// 2. Quote the payment (FX markup + commission applied here).
PrecheckResult quote = await client.PrecheckAsync(new PrecheckRequest
{
    ThirdPartyIdDebit = "bank-beta",
    ServiceId         = "card-payout",
    RecipientId       = "recipient-9999",
    AmountCredit      = 100.00m,
    CurrencyCredit    = "USD",
});
Console.WriteLine($"Rate {quote.Rate}, debit {quote.AmountDebit} {quote.CurrencyDebit}, commission {quote.Commission}");

// 3. Initiate the payment. Pass an Idempotency-Key so retries are safe.
long transactionId = await client.PayAsync(new PayRequest
{
    ThirdPartyIdDebit = "bank-beta",
    ServiceId         = "card-payout",
    SenderId          = "sender-0001",
    RecipientId       = "recipient-9999",
    AmountCredit      = 100.00m,
    CurrencyCredit    = "USD",
    Data              = new Dictionary<string, object?> { ["passport"] = "AA1234567" },
}, idempotencyKey: Guid.NewGuid().ToString());

// 4. Poll until the payment reaches a terminal status.
PostcheckResult result = await client.WaitForCompletionAsync(transactionId);
Console.WriteLine($"Final status: {result.Status}");
```

## Dependency injection (ASP.NET Core)

```csharp
using Milkyway.Payments.Sdk.DependencyInjection;

builder.Services.AddMilkywayPayments(o =>
{
    o.BaseUrl      = builder.Configuration["Milkyway:BaseUrl"]!;
    o.TokenUrl     = builder.Configuration["Milkyway:TokenUrl"]!;
    o.ClientId     = builder.Configuration["Milkyway:ClientId"]!;
    o.ClientSecret = builder.Configuration["Milkyway:ClientSecret"]!;
});

// Then inject IMilkywayPaymentsClient anywhere.
public sealed class PayoutService(IMilkywayPaymentsClient milkyway) { /* ... */ }
```

## The `data` field

Each service requires extra per-partner fields (sender name, document number,
birthday, …) in the `Data` dictionary. **Which keys are required depends on your
`ServiceId` and the recipient bank** — look them up in the Услуги registry at
<https://milkyway-docs.stage.planet9.ae>. The server validates `data` against the
service's JSON Schema during Precheck, so a missing field is rejected before any
money moves.

## Errors

All API errors throw a subclass of `MilkywayApiException` (carrying `StatusCode`
and the server's message):

| HTTP | Exception | Meaning |
| --- | --- | --- |
| 400 | `MilkywayValidationException` | Bad request (invalid amount, missing field, unresolvable FX rate). |
| 401 | `MilkywayAuthException` | Token missing/invalid (also thrown if token acquisition fails). |
| 402 | `MilkywayExposureBlockedException` | Payment would breach a block-action exposure limit. |
| 404 | `MilkywayNotFoundException` | Transaction not found or not owned by your institution. |
| 5xx | `MilkywayServiceUnavailableException` | API or downstream recipient unavailable (retried automatically first). |

## Retries & idempotency

Transient failures are retried automatically with exponential backoff + jitter
(tunable via `MilkywayOptions.MaxRetries` / `RetryBaseDelay`). **`PayAsync` is only
auto-retried when you supply an `idempotencyKey`** — without one, a retry could
create a duplicate payment, so the SDK sends it exactly once.

## Configuration

| Option | Default | Purpose |
| --- | --- | --- |
| `BaseUrl` | — (required) | Payments API base URL. |
| `TokenUrl` | — (required) | Keycloak token endpoint. |
| `ClientId` / `ClientSecret` | — (required) | Your institution's credentials. |
| `Scope` | none | Optional OAuth scope. |
| `TokenRefreshSkew` | 30s | Refresh this long before token expiry. |
| `RequestTimeout` | 30s | Per-attempt request timeout. |
| `MaxRetries` | 3 | Max transient-failure retries. |
| `RetryBaseDelay` | 500ms | Base delay for exponential backoff. |

## Building from source

```bash
dotnet build
dotnet test
dotnet pack src/Milkyway.Payments.Sdk -c Release   # produces the NuGet package
```

## Releasing

Releases are **fully automated** by [semantic-release](https://semantic-release.gitbook.io/)
on every push to `main`:

1. Conventional commits are analysed (`feat:` → minor, `fix:`/`perf:` → patch,
   `!` / `BREAKING CHANGE` → major). No releasable commits → no release.
2. The package is packed with the computed version and pushed to **NuGet.org via
   [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)**
   (OIDC — no long-lived API key stored anywhere).
3. A GitHub release + `vX.Y.Z` tag is created with generated notes.

One-time setup (maintainers): on nuget.org → **Trusted Publishing**, add a policy
for owner `bankplanet9`, repo `milkyway-csharp-sdk`, workflow file `ci.yml`. The
nuget.org user is hardcoded in the workflow; no secrets or variables are required.

## License

MIT — see [LICENSE](LICENSE).
