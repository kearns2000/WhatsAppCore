using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WhatsApp.Core.AspNetCore.Diagnostics;
using WhatsApp.Core.AspNetCore.Internal;
using WhatsApp.Core.AspNetCore.Options;
using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.AspNetCore.Dispatch;

/// <summary>
/// The default <see cref="IWhatsAppWebhookReceiver"/>. For each event, checks the configured
/// <see cref="IWhatsAppWebhookDeduplicator"/>, then resolves and invokes every
/// <see cref="IWhatsAppWebhookHandler{TEvent}"/> registered for the event's concrete runtime
/// type from a fresh dependency injection scope.
/// </summary>
/// <remarks>
/// Each event is dispatched independently: a failure in the deduplicator or in one handler is
/// logged (event type and safe ids only) and does not prevent other handlers for the same
/// event, or other events in the same delivery, from running.
/// </remarks>
public sealed class WhatsAppWebhookReceiver : IWhatsAppWebhookReceiver
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WhatsAppWebhookReceiver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppWebhookReceiver"/> class.
    /// </summary>
    /// <param name="scopeFactory">Used to create a fresh dependency injection scope per event, so handlers can depend on scoped services.</param>
    /// <param name="logger">The logger used to record deduplicator and handler failures.</param>
    public WhatsAppWebhookReceiver(
        IServiceScopeFactory scopeFactory,
        ILogger<WhatsAppWebhookReceiver> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ReceiveAsync(
        IReadOnlyList<WhatsAppWebhookEvent> events,
        WhatsAppWebhookOptions options,
        CancellationToken stopToken)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(options);
        if (events.Count == 0)
        {
            return;
        }

        if (options.DispatchMode == WhatsAppWebhookDispatchMode.Parallel)
        {
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = stopToken,
                MaxDegreeOfParallelism = Math.Max(1, options.MaxDegreeOfParallelism),
            };

            await Parallel.ForEachAsync(
                events,
                parallelOptions,
                (notification, ct) => new ValueTask(DispatchOneAsync(notification, ct))).ConfigureAwait(false);
        }
        else
        {
            foreach (var notification in events)
            {
                await DispatchOneAsync(notification, stopToken).ConfigureAwait(false);
            }
        }
    }

    private async Task DispatchOneAsync(WhatsAppWebhookEvent notification, CancellationToken stopToken)
    {
        using var activity = WhatsAppWebhookDiagnostics.StartDispatchActivity(notification.GetType().Name);

        await using var scope = _scopeFactory.CreateAsyncScope();

        bool accepted;
        try
        {
            var deduplicator = scope.ServiceProvider.GetRequiredService<IWhatsAppWebhookDeduplicator>();
            accepted = await deduplicator.TryAcceptAsync(notification, stopToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            WhatsAppWebhookLog.DeduplicatorFailed(_logger, ex, "IWhatsAppWebhookDeduplicator", notification.GetType().Name, GetSafeMessageId(notification));
            WhatsAppWebhookDiagnostics.RecordDispatchFailure(notification.GetType().Name);
            return;
        }

        if (!accepted)
        {
            return;
        }

        var handlerInterfaceType = WhatsAppWebhookHandlerInvocation.GetHandlerInterfaceType(notification.GetType());
        var handlers = scope.ServiceProvider.GetServices(handlerInterfaceType);

        foreach (var handler in handlers)
        {
            if (handler is null)
            {
                continue;
            }

            try
            {
                await WhatsAppWebhookHandlerInvocation
                    .InvokeAsync(handler, handlerInterfaceType, notification, stopToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                WhatsAppWebhookLog.HandlerFailed(_logger, ex, handler.GetType().Name, notification.GetType().Name, GetSafeMessageId(notification));
                WhatsAppWebhookDiagnostics.RecordDispatchFailure(notification.GetType().Name);

                // Log and continue: isolate this handler's failure from other handlers of the
                // same event and from other events in this delivery.
            }
        }
    }

    private static string? GetSafeMessageId(WhatsAppWebhookEvent notification) => notification switch
    {
        WhatsAppMessageEvent messageEvent => messageEvent.MessageId,
        WhatsAppMessageStatusEvent statusEvent => statusEvent.MessageId,
        _ => null,
    };
}
