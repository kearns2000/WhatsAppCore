# Error Handling

WhatsApp.Core distinguishes between validation errors (caught before any HTTP call) and API errors (returned by Meta's Graph API). Both exception types carry enough detail for logging and retry decisions without exposing secrets.

## Exception types

### WhatsAppValidationException

Thrown when client-side validation fails. No network request is made.

Common causes:

- Missing or empty recipient phone number
- Empty text message body
- Missing template name or language code
- Both `mediaId` and `link` provided (or neither) for media messages
- Missing reaction target message id
- Invalid latitude or longitude
- Empty interactive message sections
- Recipient contains formatting characters

```csharp
try
{
    await client.SendTextAsync("", "", stopToken: stopToken);
}
catch (WhatsAppValidationException ex)
{
    // ex.Message describes the validation failure
}
```

These are programmer or input errors. Retrying without fixing the request will not help.

### WhatsAppApiException

Thrown when the Graph API returns a non-success HTTP status. Parsed from Meta's error JSON when possible.

| Property | Description |
|----------|-------------|
| `StatusCode` | HTTP status code |
| `ErrorCode` | Graph API `error.code` |
| `ErrorSubcode` | Graph API `error.error_subcode` |
| `ErrorType` | Graph API `error.type` |
| `MetaTraceId` | `error.fbtrace_id` - useful when contacting Meta support |
| `IsTransient` | Whether the error may succeed on retry |
| `RetryAfter` | Suggested delay from `Retry-After` header |
| `ErrorData` | Additional structured error data (`error.error_data`) |

```csharp
try
{
    await client.SendTemplateAsync("353...", "nonexistent_template", "en_US", stopToken: stopToken);
}
catch (WhatsAppApiException ex)
{
    logger.LogWarning(
        "WhatsApp API error. Status={Status}, Code={Code}, TraceId={TraceId}, Transient={Transient}",
        (int)ex.StatusCode,
        ex.ErrorCode,
        ex.MetaTraceId,
        ex.IsTransient);
}
```

Exception messages and `ErrorData` never include access tokens or app secrets.

## HTTP status handling

| Status | Typical meaning | Retry? |
|--------|-----------------|--------|
| 400 | Bad request - invalid payload or parameters | No (fix the request) |
| 401 | Authentication failure - invalid or expired token | No (refresh token) |
| 403 | Permission denied | No |
| 404 | Resource not found (media id, phone number id) | No |
| 408 | Request timeout | Maybe (transient) |
| 429 | Rate limited | Yes, after `RetryAfter` |
| 500-599 | Server error | Maybe (transient) |

The library parses error JSON when present, handles empty or HTML error bodies gracefully, and preserves `Retry-After` from response headers.

## Retry policy

### Message sends - never auto-retried

```csharp
// NOT retried automatically - by design
await client.SendTextAsync(...);
await client.SendTemplateAsync(...);
await client.UploadMediaAsync(...);
```

If a send fails with a transient error, you may choose to retry manually - but understand the duplicate delivery risk. Consider idempotency keys in your application layer if you implement retries.

### Idempotent operations - opt-in safe retries

Enable for media metadata GET and media DELETE only:

```json
{
  "WhatsApp": {
    "Resilience": {
      "EnableSafeRetries": true,
      "MaxSafeRetries": 2
    }
  }
}
```

When enabled, transient failures (408, 429, 5xx) on eligible operations are retried up to `MaxSafeRetries` times, respecting `Retry-After` when present.

### Implementing your own retry

Use exception metadata to decide:

```csharp
catch (WhatsAppApiException ex) when (ex.IsTransient == true)
{
    TimeSpan delay = ex.RetryAfter ?? TimeSpan.FromSeconds(5);
    await Task.Delay(delay, stopToken);
    // Retry with application-level idempotency awareness
}
```

## Cancellation

Pass `CancellationToken` to every async operation. Caller-requested cancellation propagates correctly and is **not** converted into timeout exceptions.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await client.SendTextAsync("353...", "Hello", stopToken: cts.Token);
```

## Webhook handler errors

When a typed webhook handler throws, the default receiver:

1. Logs the failure with event type and message id (not message content).
2. Continues processing remaining events.
3. Still returns 200 to Meta (the webhook was received and parsed successfully).

For critical handler failures, implement your own `IWhatsAppWebhookReceiver` with dead-letter handling.

## Sample API error responses

The sample application maps exceptions to HTTP responses:

- `WhatsAppValidationException` → 400 Bad Request
- `WhatsAppApiException` → Problem Details with status code, error code, trace id, and retry metadata

See `samples/WhatsApp.Core.Sample.Api/Program.cs` for the mapping implementation.

## Logging guidance

Safe to log:

- HTTP status codes
- Meta error codes and trace ids
- Event types and message ids
- Operation names and account names

Never log by default:

- Access tokens, app secrets, verify tokens
- Message body text
- Complete phone numbers
- Temporary media download URLs
- Webhook signatures

## Related documentation

- [Architecture](architecture.md) - Why message sends are not retried
- [Configuration](configuration.md) - Resilience options
- [Observability](observability.md) - Failure metrics
