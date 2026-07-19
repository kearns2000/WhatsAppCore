using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using WhatsApp.Core.AspNetCore.Dispatch;
using WhatsApp.Core.AspNetCore.Options;
using WhatsApp.Core.AspNetCore.Webhooks;

namespace WhatsApp.Core.AspNetCore.DependencyInjection;

/// <summary>
/// Extension methods for registering WhatsApp webhook receiving, signature validation, and
/// event dispatch with an <see cref="IServiceCollection"/>.
/// </summary>
public static class WhatsAppWebhookServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services required to receive and dispatch WhatsApp webhooks: options
    /// validation, an in-memory <see cref="IWhatsAppWebhookDeduplicator"/> (replace it by
    /// registering your own before calling this method, or by overriding it afterwards; use
    /// <see cref="NoOpWhatsAppWebhookDeduplicator"/> only when handlers are idempotent), and the
    /// default <see cref="IWhatsAppWebhookReceiver"/>. Call this once, then map one or more
    /// endpoints with <c>MapWhatsAppWebhook</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An optional delegate that populates <see cref="WhatsAppWebhookOptions"/>.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddWhatsAppWebhooks(
        this IServiceCollection services, Action<WhatsAppWebhookOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<WhatsAppWebhookOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<WhatsAppWebhookOptions>, WhatsAppWebhookOptionsValidator>());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IWhatsAppWebhookDeduplicator>(provider =>
            new MemoryWhatsAppWebhookDeduplicator(provider.GetRequiredService<TimeProvider>()));
        services.TryAddScoped<IWhatsAppWebhookReceiver, WhatsAppWebhookReceiver>();

        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="THandler"/> to handle every <typeparamref name="TEvent"/>
    /// dispatched by the default <see cref="IWhatsAppWebhookReceiver"/>. Multiple handlers,
    /// including for different <typeparamref name="TEvent"/> types, may be registered; all are
    /// invoked.
    /// </summary>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <typeparam name="TEvent">The concrete event type the handler processes.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddWhatsAppWebhookHandler<THandler, TEvent>(this IServiceCollection services)
        where THandler : class, IWhatsAppWebhookHandler<TEvent>
        where TEvent : WhatsAppWebhookEvent
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IWhatsAppWebhookHandler<TEvent>, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a pre-built <paramref name="handler"/> instance to handle every
    /// <typeparamref name="TEvent"/> dispatched by the default
    /// <see cref="IWhatsAppWebhookReceiver"/>. Useful for simple handlers (e.g. a lambda wrapped
    /// in a small adapter) that do not need their own dependency-injected lifetime.
    /// </summary>
    /// <typeparam name="TEvent">The concrete event type the handler processes.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="handler">The handler instance, registered as a singleton.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddWhatsAppWebhookHandler<TEvent>(
        this IServiceCollection services, IWhatsAppWebhookHandler<TEvent> handler)
        where TEvent : WhatsAppWebhookEvent
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handler);

        services.AddSingleton(handler);
        return services;
    }
}
