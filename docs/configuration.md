# Configuration

WhatsApp.Core configuration is validated at startup through the .NET Options pattern. Invalid settings cause the application to fail fast rather than failing on the first API call.

## Configuration section

The default account binds from a configuration section named `WhatsApp`:

```json
{
  "WhatsApp": {
    "PhoneNumberId": "123456789012345",
    "AccessToken": "EAA...",
    "AppSecret": "abc123...",
    "VerifyToken": "my-random-verify-token",
    "GraphApiVersion": "v21.0"
  }
}
```

Register in your application:

```csharp
using WhatsApp.Core.DependencyInjection;

builder.Services.AddWhatsAppCore(
    builder.Configuration.GetSection("WhatsApp"));
```

## WhatsAppOptions

| Property | Required | Default | Description |
|----------|----------|---------|-------------|
| `PhoneNumberId` | Yes | - | Meta-assigned phone number ID (not the phone number itself) |
| `AccessToken` | Yes | - | Bearer token for Graph API authentication |
| `AppSecret` | For webhooks | `null` | Used to validate `X-Hub-Signature-256` on inbound webhooks |
| `VerifyToken` | For webhooks | `null` | Token Meta sends during the subscription handshake |
| `GraphApiVersion` | Yes | `v21.0` | Pinned Graph API version (must not be `latest`) |
| `BaseAddress` | No | `https://graph.facebook.com/` | Graph API base URL |
| `Timeout` | No | 30 seconds | `HttpClient` timeout |
| `AllowInsecureHttp` | No | `false` | Allow non-HTTPS base address (testing only; emits a warning) |
| `AllowedMediaDownloadHosts` | No | empty | Extra hosts `DownloadMediaAsync` may follow (tests) |
| `Resilience` | No | See below | Opt-in safe retry settings |

### Programmatic configuration

```csharp
builder.Services.AddWhatsAppCore(options =>
{
    options.PhoneNumberId = Environment.GetEnvironmentVariable("WHATSAPP_PHONE_NUMBER_ID")!;
    options.AccessToken = Environment.GetEnvironmentVariable("WHATSAPP_ACCESS_TOKEN")!;
    options.AppSecret = Environment.GetEnvironmentVariable("WHATSAPP_APP_SECRET");
    options.VerifyToken = Environment.GetEnvironmentVariable("WHATSAPP_VERIFY_TOKEN");
    options.GraphApiVersion = "v21.0";
});
```

### Named accounts

Bind multiple accounts from nested configuration:

```json
{
  "WhatsApp": {
    "Accounts": {
      "support": {
        "PhoneNumberId": "...",
        "AccessToken": "...",
        "GraphApiVersion": "v21.0"
      },
      "marketing": {
        "PhoneNumberId": "...",
        "AccessToken": "...",
        "GraphApiVersion": "v21.0"
      }
    }
  }
}
```

```csharp
builder.Services.AddWhatsAppCore("support",
    builder.Configuration.GetSection("WhatsApp:Accounts:support"));

builder.Services.AddWhatsAppCore("marketing",
    builder.Configuration.GetSection("WhatsApp:Accounts:marketing"));
```

Resolve clients:

```csharp
IWhatsAppClientFactory factory = services.GetRequiredService<IWhatsAppClientFactory>();
IWhatsAppClient support = factory.CreateClient("support");
```

## WhatsAppResilienceOptions

Nested under `WhatsApp:Resilience`:

```json
{
  "WhatsApp": {
    "Resilience": {
      "EnableSafeRetries": false,
      "MaxSafeRetries": 2
    }
  }
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `EnableSafeRetries` | `false` | Retry idempotent GET/DELETE media operations on transient failures |
| `MaxSafeRetries` | `2` | Maximum additional attempts after the initial request |

**Message sends and media uploads are never retried**, regardless of this setting.

## WhatsAppWebhookOptions

Configure webhook endpoint behavior via `AddWhatsAppWebhooks`:

```csharp
using WhatsApp.Core.AspNetCore.DependencyInjection;

builder.Services.AddWhatsAppWebhooks(options =>
{
    options.AccountName = null; // default account
    options.RequireSignatureValidation = true;
    options.MaxRequestBodyBytes = 1_048_576; // 1 MiB
    options.DispatchMode = WhatsAppWebhookDispatchMode.Sequential;
    options.MaxDegreeOfParallelism = 4;
});
```

| Property | Default | Description |
|----------|---------|-------------|
| `AccountName` | `null` (default account) | Which registered account's secrets to use |
| `RequireSignatureValidation` | `true` | Require valid `X-Hub-Signature-256` on POST |
| `AllowInsecureNoSignatureValidation` | `false` | Required opt-in before `RequireSignatureValidation` may be `false` |
| `MaxRequestBodyBytes` | 1,048,576 (1 MiB) | Maximum accepted webhook body size |
| `DispatchMode` | `Sequential` | `Sequential` or `Parallel` |
| `MaxDegreeOfParallelism` | `4` | Max concurrent handler invocations in parallel mode |

Per-endpoint overrides:

```csharp
app.MapWhatsAppWebhook("/webhooks/support", options =>
{
    options.AccountName = "support";
});

app.MapWhatsAppWebhook("/webhooks/marketing", options =>
{
    options.AccountName = "marketing";
});
```

## Secret management

Never commit real credentials. Recommended approaches:

| Environment | Approach |
|-------------|----------|
| Local development | [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) |
| Production | Azure Key Vault, AWS Secrets Manager, environment variables |
| CI/CD | Encrypted pipeline secrets |

The sample application uses `appsettings.Development.json` with `REPLACE_ME` placeholders and a user secrets ID for local overrides.

## Validation rules

Startup validation enforces:

- `PhoneNumberId` and `AccessToken` are non-empty.
- `GraphApiVersion` is non-empty and is not `latest`.
- `BaseAddress` uses HTTPS unless `AllowInsecureHttp` is `true` (which also emits a warning).
- Disabling signature validation requires `AllowInsecureNoSignatureValidation = true` and emits a warning.

`AppSecret` and `VerifyToken` are optional unless you map webhook endpoints. If they are missing when webhooks are enabled, verification and signature validation will fail at runtime.

## Graph API version

The default version is defined in a single internal constant (`v21.0`). Always pin an explicit version in configuration. Meta deprecates versions on a published schedule; using `latest` would allow silent breaking changes.

Monitor [Meta's changelog](https://developers.facebook.com/docs/graph-api/changelog) and update `GraphApiVersion` when upgrading.

## Related documentation

- [Architecture](architecture.md)
- [Webhooks](webhooks.md)
- [Security guidance](../SECURITY.md)
