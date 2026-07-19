using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsApp.Core.AspNetCore.Diagnostics;
using WhatsApp.Core.AspNetCore.Dispatch;
using WhatsApp.Core.AspNetCore.Internal;
using WhatsApp.Core.AspNetCore.Options;
using WhatsApp.Core.AspNetCore.Signature;
using WhatsApp.Core.Configuration;

namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// Implements the GET verification handshake and POST delivery handling behind
/// <see cref="WhatsAppWebhookEndpointRouteBuilderExtensions.MapWhatsAppWebhook(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder, string)"/>.
/// </summary>
internal static class WhatsAppWebhookEndpointHandlers
{
    private const int MaxCopyBufferSize = 81_920;
    private const string LoggerCategoryName = "WhatsApp.Core.AspNetCore.Webhooks";

    private static ILogger CreateLogger(HttpContext context) =>
        context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(LoggerCategoryName);

    /// <summary>
    /// Handles Meta's webhook subscription verification handshake
    /// (<c>hub.mode</c>/<c>hub.verify_token</c>/<c>hub.challenge</c>).
    /// </summary>
    public static IResult HandleVerification(HttpContext context, WhatsAppWebhookOptions webhookOptions)
    {
        var logger = CreateLogger(context);
        var route = context.Request.Path.Value ?? webhookOptions.AccountName ?? "/";

        var mode = context.Request.Query["hub.mode"].ToString();
        var suppliedToken = context.Request.Query["hub.verify_token"].ToString();
        var challenge = context.Request.Query["hub.challenge"].ToString();

        var accountName = webhookOptions.AccountName ?? WhatsAppOptions.DefaultAccountName;
        var whatsAppOptions = context.RequestServices
            .GetRequiredService<IOptionsMonitor<WhatsAppOptions>>()
            .Get(accountName);

        if (string.Equals(mode, "subscribe", StringComparison.Ordinal)
            && ConstantTimeEquals.StringsEqual(suppliedToken, whatsAppOptions.VerifyToken))
        {
            WhatsAppWebhookLog.VerificationSucceeded(logger, route, mode);
            return Results.Text(challenge, "text/plain");
        }

        // Never echo the expected token back; a generic failure response is all the caller gets.
        WhatsAppWebhookLog.VerificationFailed(logger, route, string.IsNullOrEmpty(mode) ? null : mode);
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Handles an inbound webhook delivery: enforces the body size limit, validates the
    /// signature, parses the payload, and dispatches the resulting events.
    /// </summary>
    public static async Task<IResult> HandleDeliveryAsync(HttpContext context, WhatsAppWebhookOptions webhookOptions)
    {
        var logger = CreateLogger(context);
        var route = context.Request.Path.Value ?? webhookOptions.AccountName ?? "/";
        using var receiveActivity = WhatsAppWebhookDiagnostics.StartReceiveActivity(route);

        var accountName = webhookOptions.AccountName ?? WhatsAppOptions.DefaultAccountName;
        var whatsAppOptions = context.RequestServices
            .GetRequiredService<IOptionsMonitor<WhatsAppOptions>>()
            .Get(accountName);

        ApplyMaxRequestBodySize(context, webhookOptions.MaxRequestBodyBytes);

        if (context.Request.ContentLength is { } contentLength && contentLength > webhookOptions.MaxRequestBodyBytes)
        {
            WhatsAppWebhookLog.PayloadTooLarge(logger, route, webhookOptions.MaxRequestBodyBytes);
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        byte[] body;
        try
        {
            body = await ReadBodyAsync(context.Request, webhookOptions.MaxRequestBodyBytes, context.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (WhatsAppWebhookPayloadTooLargeException)
        {
            WhatsAppWebhookLog.PayloadTooLarge(logger, route, webhookOptions.MaxRequestBodyBytes);
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        WhatsAppWebhookDiagnostics.RecordWebhookReceived(route);
        WhatsAppWebhookLog.WebhookReceived(logger, route, body.LongLength);

        // Skip HMAC only when both the disable flag and the explicit insecure opt-in are set.
        // Per-endpoint snapshots are also gated by WhatsAppWebhookSignaturePolicy.EnsureMappable.
        var environmentName = context.RequestServices
            .GetRequiredService<IHostEnvironment>()
            .EnvironmentName;
        if (!WhatsAppWebhookSignaturePolicy.MaySkipSignatureValidation(webhookOptions, environmentName))
        {
            using var signatureActivity = WhatsAppWebhookDiagnostics.StartValidateSignatureActivity();

            if (string.IsNullOrWhiteSpace(whatsAppOptions.AppSecret))
            {
                WhatsAppWebhookLog.AppSecretNotConfigured(logger, route, accountName);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }

            var suppliedSignature = context.Request.Headers[WhatsAppWebhookSignature.HeaderName].ToString();
            IWhatsAppWebhookSignatureValidator signatureValidator = new WhatsAppWebhookSignatureValidator(whatsAppOptions.AppSecret);
            if (!signatureValidator.IsValid(body, suppliedSignature))
            {
                WhatsAppWebhookLog.SignatureValidationFailed(logger, route);
                WhatsAppWebhookDiagnostics.RecordInvalidSignature(route);
                return Results.StatusCode(StatusCodes.Status401Unauthorized);
            }
        }

        IReadOnlyList<WhatsAppWebhookEvent> events;
        try
        {
            using var parseActivity = WhatsAppWebhookDiagnostics.StartParseActivity();
            var timeProvider = context.RequestServices.GetService<TimeProvider>() ?? TimeProvider.System;
            events = WhatsAppWebhookEnvelopeParser.Parse(body, timeProvider.GetUtcNow());
        }
        catch (JsonException ex)
        {
            WhatsAppWebhookLog.WebhookParseFailed(logger, ex, route);
            return Results.StatusCode(StatusCodes.Status400BadRequest);
        }

        WhatsAppWebhookLog.WebhookParsed(logger, route, events.Count);
        WhatsAppWebhookDiagnostics.RecordEventsParsed(events.Count);

        var receiver = context.RequestServices.GetRequiredService<IWhatsAppWebhookReceiver>();
        await receiver.ReceiveAsync(events, webhookOptions, context.RequestAborted).ConfigureAwait(false);

        return Results.Ok();
    }

    private static void ApplyMaxRequestBodySize(HttpContext context, long maxRequestBodyBytes)
    {
        var maxBodyFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (maxBodyFeature is { IsReadOnly: false })
        {
            maxBodyFeature.MaxRequestBodySize = maxRequestBodyBytes;
        }
    }

    private static async Task<byte[]> ReadBodyAsync(HttpRequest request, long maxBytes, CancellationToken stopToken)
    {
        var initialCapacity = request.ContentLength is { } declaredLength && declaredLength > 0 && declaredLength <= maxBytes
            ? (int)declaredLength
            : Math.Min(4096, (int)Math.Max(1, maxBytes));

        using var buffer = new MemoryStream(initialCapacity);
        var chunk = new byte[MaxCopyBufferSize];
        long total = 0;

        int read;
        while ((read = await request.Body.ReadAsync(chunk, stopToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                throw new WhatsAppWebhookPayloadTooLargeException();
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
