using System.Collections.Concurrent;
using System.Reflection;
using WhatsApp.Core.AspNetCore.Dispatch;
using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.AspNetCore.Internal;

/// <summary>
/// Caches the reflection metadata needed to resolve and invoke
/// <see cref="IWhatsAppWebhookHandler{TEvent}"/> instances for a runtime-known concrete
/// <see cref="WhatsAppWebhookEvent"/> type, since the generic handler interface cannot be
/// referenced directly without knowing <c>TEvent</c> at compile time.
/// </summary>
internal static class WhatsAppWebhookHandlerInvocation
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerInterfaceTypes = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> HandleAsyncMethods = new();

    /// <summary>
    /// Gets the closed <see cref="IWhatsAppWebhookHandler{TEvent}"/> type for the given concrete
    /// event type, e.g. <c>IWhatsAppWebhookHandler&lt;WhatsAppTextMessageEvent&gt;</c>.
    /// </summary>
    /// <param name="eventType">The concrete, runtime <see cref="WhatsAppWebhookEvent"/> type.</param>
    /// <returns>The closed handler interface type, usable with <see cref="IServiceProvider.GetService"/>.</returns>
    public static Type GetHandlerInterfaceType(Type eventType) =>
        HandlerInterfaceTypes.GetOrAdd(eventType, static t => typeof(IWhatsAppWebhookHandler<>).MakeGenericType(t));

    /// <summary>
    /// Invokes <c>HandleAsync</c> on <paramref name="handler"/>.
    /// </summary>
    /// <param name="handler">A handler instance implementing <paramref name="handlerInterfaceType"/>.</param>
    /// <param name="handlerInterfaceType">The closed handler interface type, from <see cref="GetHandlerInterfaceType"/>.</param>
    /// <param name="notification">The event to pass to the handler.</param>
    /// <param name="stopToken">A token that is canceled if the underlying HTTP request is aborted.</param>
    /// <returns>The task returned by the handler's <c>HandleAsync</c> method.</returns>
    public static Task InvokeAsync(object handler, Type handlerInterfaceType, WhatsAppWebhookEvent notification, CancellationToken stopToken)
    {
        var method = HandleAsyncMethods.GetOrAdd(
            handlerInterfaceType,
            static t => t.GetMethod(nameof(IWhatsAppWebhookHandler<WhatsAppWebhookEvent>.HandleAsync))
                ?? throw new MissingMethodException(t.FullName, nameof(IWhatsAppWebhookHandler<WhatsAppWebhookEvent>.HandleAsync)));

        return (Task)method.Invoke(handler, [notification, stopToken])!;
    }
}
