# MilkyWay Payments SDK for .NET

[![NuGet](https://img.shields.io/nuget/v/Milkyway.Payments.Sdk.svg?logo=nuget)](https://www.nuget.org/packages/Milkyway.Payments.Sdk/)
[![Downloads](https://img.shields.io/nuget/dt/Milkyway.Payments.Sdk.svg?logo=nuget)](https://www.nuget.org/packages/Milkyway.Payments.Sdk/)
[![.NET](https://img.shields.io/badge/.NET-net8.0%20%7C%20netstandard2.0-512BD4)](https://www.nuget.org/packages/Milkyway.Payments.Sdk/)
[![CI](https://github.com/bankplanet9/milkyway-csharp-sdk/actions/workflows/ci.yml/badge.svg)](https://github.com/bankplanet9/milkyway-csharp-sdk/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

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

The package is on [NuGet.org](https://www.nuget.org/packages/Milkyway.Payments.Sdk/) — install it with whichever tool you use:

```bash
# .NET CLI
dotnet add package Milkyway.Payments.Sdk
```

```powershell
# Package Manager Console (Visual Studio)
Install-Package Milkyway.Payments.Sdk
```

```xml
<!-- PackageReference in your .csproj -->
<PackageReference Include="Milkyway.Payments.Sdk" Version="1.0.0" />
```

Targets `net8.0` and `netstandard2.0`, so it works on modern .NET as well as
.NET Framework 4.6.1+, Mono, and Xamarin. No other setup — bring your Keycloak
`ClientId`/`ClientSecret` and you're ready.

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

// Your customer (the payer). Fill what you hold; the service's PartyProfile in
// discovery says which fields the recipient bank requires.
var sender = new PartyInfo
{
    PartyType               = PartyType.Natural,
    Name                    = new PartyName { First = "Ivan", Last = "Petrov", Full = "Petrov Ivan Sergeevich" },
    DateOfBirth             = "1990-04-18",
    IdentityDocumentType    = IdentityDocumentType.Passport,
    IdentityDocumentNumber  = "AB1234567",
    IdentityDocumentCountry = "TJ",
    CountryOfResidence      = "AE",
};

// 2. Quote the payment (FX markup + commission applied here).
PrecheckResult quote = await client.PrecheckAsync(new PrecheckRequest
{
    ThirdPartyIdDebit = "bank-beta",
    ServiceId         = "card-payout",
    RecipientId       = "recipient-9999",
    AmountCredit      = 100.00m,
    CurrencyCredit    = "USD",
    SenderInfo        = sender,
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
    SenderInfo        = sender,
}, idempotencyKey: Guid.NewGuid().ToString());

// 4. Poll until the payment reaches a terminal status.
PostcheckResult result = await client.WaitForCompletionAsync(transactionId);
Console.WriteLine($"Final status: {result.Status}");
```

## Discovery

Before quoting or paying, you can browse the catalog of recipient banks and the
services each offers — two read-only calls:

```csharp
// List every destination, or filter by country (ISO 3166-1 alpha-2, case-insensitive).
IReadOnlyList<Destination> destinations = await client.DiscoveryAsync("TJ");
foreach (var d in destinations)
{
    Console.WriteLine($"{d.ThirdPartyId} ({d.PartnerName}, {d.Country})");
    foreach (var s in d.Services)
    {
        Console.WriteLine($"  {s.ServiceId} via {s.PayoutMethod} → {string.Join(", ", s.PayoutCurrencies ?? Array.Empty<string>())}");
        // Which SenderInfo fields the recipient bank requires, e.g. name.first, date_of_birth.
        Console.WriteLine($"    sender requires: {string.Join(", ", s.PartyProfile?.SenderInfo?.Required ?? Array.Empty<string>())}");
    }
}

// Fetch the Draft-2020 JSON Schema a service validates its `data` extras against
// (most services take none).
JsonElement schema = await client.ServiceSchemaAsync("bank-beta", "card-payout");
Console.WriteLine(schema.GetProperty("required").GetArrayLength());
```

`DiscoveryAsync` unwraps the `destinations` list for you. Each service carries its
`PartyProfile` — which `SenderInfo` / `RecipientInfo` fields the recipient bank
requires and accepts (see below). The per-service JSON Schema for `data` is omitted
from the catalog and fetched on demand with `ServiceSchemaAsync` (returned as a
`System.Text.Json.JsonElement`). Both throw `MilkywayAuthException`
on `401`; `ServiceSchemaAsync` throws `MilkywayNotFoundException` when the
bank/service pair is unknown.

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

## Payer and beneficiary (`SenderInfo`, `RecipientInfo`)

The identity of your customer (`SenderInfo`, the originator) and of the beneficiary
(`RecipientInfo`) travels in two typed `PartyInfo` objects that are **the same for
every recipient bank and service** (FATF Travel Rule / ISO 20022 / IVMS101
originator–beneficiary data). Map your customer data to it once; each recipient
bank's connector converts it to that bank's format. The account, card or phone stays
in `SenderId` / `RecipientId` — these objects carry the person.

| Property | Wire field | Format |
| --- | --- | --- |
| `PartyType` | `party_type` | `PartyType.Natural` (the default, always sent). `PartyType.Legal` is not accepted by the API yet. |
| `Name.First`, `Name.Last`, `Name.Full` | `name.first`, `name.last`, `name.full` | Text. `Full` is the name as written in the document, family name first, with a patronymic if any. |
| `DateOfBirth` | `date_of_birth` | ISO 8601 date, `YYYY-MM-DD`. |
| `IdentityDocumentType` | `identity_document_type` | An `IdentityDocumentType` constant: `Passport`, `NationalId`, `DriverLicense`, `ResidencePermit`, `TaxId`, `SocialSecurity`, `Other`. Required with `IdentityDocumentNumber`. |
| `IdentityDocumentNumber` | `identity_document_number` | Series and number together, as in the document. |
| `IdentityDocumentCountry`, `CountryOfResidence` | `identity_document_country`, `country_of_residence` | ISO 3166-1 alpha-2, upper case (`TJ`). |
| `Address` | `address` | `Country` (required when an address is sent), `City`, `Street`, `Building`, `PostalCode`, and `Line` — up to 7 free-text lines. |

Text values are at most 256 characters. Null and empty properties are left off the
wire. The server refuses a value of the wrong type or an invalid value with `400`
(`MilkywayValidationException`) naming the field, e.g.
`sender_info.identity_document_type`.

**Which fields a service needs** is set by the recipient bank and published per
service as `ServiceCapability.PartyProfile` in `DiscoveryAsync`: `Required` and
`Accepted` field paths (e.g. `name.first`, `date_of_birth`) for `SenderInfo` and
`RecipientInfo`, and, when the bank takes only some document kinds,
`IdentityDocumentTypes`. A Precheck or Pay missing a required field is refused with
`400` naming it (`sender_info.date_of_birth: is required by the receiving bank for
this service`). Send the fields you have; fields the bank does not use are ignored.

```csharp
var request = new PayRequest
{
    // ...
    SenderInfo = sender,
    RecipientInfo = new PartyInfo
    {
        Name    = new PartyName { First = "Aziz", Last = "Karimov" },
        Address = new PartyAddress { Country = "TJ", City = "Dushanbe" },
    },
};
```

Identity must **not** be put in `Data`: MilkyWay never reads it from there.

## Service-specific extras (`Data`)

`Data` carries only what a service needs beyond the parties (for example a test
control field on the stage stub bank). Most services take no `Data` fields. When one
does, `ServiceSchemaAsync` returns its JSON Schema, and the recipient bank validates
it during Precheck. The human-browsable registry of banks, services, party profiles
and `data` fields is the **Handbook** at
<https://docs.planet9.ae/site/milkyway-handbook/en/> (section **Services**).

## Errors

All API errors throw a subclass of `MilkywayApiException` (carrying `StatusCode`
and the server's message):

| HTTP | Exception | Meaning |
| --- | --- | --- |
| 400 | `MilkywayValidationException` | Bad request (invalid amount, missing field — including a `SenderInfo` / `RecipientInfo` field the recipient bank requires). |
| 401 | `MilkywayAuthException` | Token missing/invalid (also thrown if token acquisition fails). |
| 402 | `MilkywayExposureBlockedException` | Payment would breach a block-action exposure limit. |
| 404 | `MilkywayNotFoundException` | Transaction not found or not owned by your institution. |
| 5xx | `MilkywayServiceUnavailableException` | API or downstream recipient unavailable, or the recipient bank quoted no usable FX rate for a cross-currency payment (retried automatically first). |

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
