using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WhatsApp.Core.AspNetCore.Options;

namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// Extension methods for mapping WhatsApp webhook endpoints onto an
/// <see cref="IEndpointRouteBuilder"/>.
/// </summary>
/// <remarks>
/// <c>AddWhatsAppWebhooks</c> (in <c>WhatsApp.Core.AspNetCore.DependencyInjection</c>) must be
/// called before mapping any endpoint, so that the services these endpoints depend on
/// (<see cref="Dispatch.IWhatsAppWebhookReceiver"/>, <see cref="Dispatch.IWhatsAppWebhookDeduplicator"/>,
/// and webhook options validation) are registered.
/// </remarks>
public static class WhatsAppWebhookEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a WhatsApp webhook endpoint at <paramref name="pattern"/>, handling both Meta's GET
    /// verification handshake and POST deliveries. The endpoint's behavior is read from the
    /// ambient, dependency-injected <see cref="WhatsAppWebhookOptions"/> (configured via
    /// <c>AddWhatsAppWebhooks</c>) on every request, so changes to those options take effect
    /// without remapping.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern to map, e.g. <c>"/webhooks/whatsapp"</c>.</param>
    /// <returns>A builder that can be used to further customize the mapped endpoints (e.g. add authorization metadata).</returns>
    public static RouteGroupBuilder MapWhatsAppWebhook(this IEndpointRouteBuilder endpoints, string pattern) =>
        MapCore(
            endpoints,
            pattern,
            static context => context.RequestServices
                .GetRequiredService<IOptionsMonitor<WhatsAppWebhookOptions>>()
                .CurrentValue);

    /// <summary>
    /// Maps a WhatsApp webhook endpoint at <paramref name="pattern"/>, using a dedicated
    /// <see cref="WhatsAppWebhookOptions"/> built by <paramref name="configure"/> instead of the
    /// ambient, dependency-injected options. Useful when mapping more than one webhook endpoint
    /// (e.g. one per WhatsApp account) with different settings.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern to map, e.g. <c>"/webhooks/whatsapp"</c>.</param>
    /// <param name="configure">A delegate that populates the options used for this endpoint only.</param>
    /// <returns>A builder that can be used to further customize the mapped endpoints (e.g. add authorization metadata).</returns>
    public static RouteGroupBuilder MapWhatsAppWebhook(
        this IEndpointRouteBuilder endpoints, string pattern, Action<WhatsAppWebhookOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new WhatsAppWebhookOptions();
        configure(options);
        return MapWhatsAppWebhook(endpoints, pattern, options);
    }

    /// <summary>
    /// Maps a WhatsApp webhook endpoint at <paramref name="pattern"/>, using
    /// <paramref name="options"/> directly instead of the ambient, dependency-injected options.
    /// Useful when mapping more than one webhook endpoint (e.g. one per WhatsApp account) with
    /// different settings.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern to map, e.g. <c>"/webhooks/whatsapp"</c>.</param>
    /// <param name="options">The options used for this endpoint only.</param>
    /// <returns>A builder that can be used to further customize the mapped endpoints (e.g. add authorization metadata).</returns>
    public static RouteGroupBuilder MapWhatsAppWebhook(
        this IEndpointRouteBuilder endpoints, string pattern, WhatsAppWebhookOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var snapshot = options.Clone();
        return MapCore(endpoints, pattern, _ => snapshot);
    }

    private static RouteGroupBuilder MapCore(
        IEndpointRouteBuilder endpoints, string pattern, Func<HttpContext, WhatsAppWebhookOptions> resolveOptions)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(resolveOptions);

        var group = endpoints.MapGroup(pattern).WithDisplayName("WhatsApp Webhook");

        Func<HttpContext, IResult> verificationHandler = context =>
            WhatsAppWebhookEndpointHandlers.HandleVerification(context, resolveOptions(context));

        Func<HttpContext, Task<IResult>> deliveryHandler = context =>
            WhatsAppWebhookEndpointHandlers.HandleDeliveryAsync(context, resolveOptions(context));

        group.MapGet(string.Empty, verificationHandler)
            .WithName("WhatsAppWebhookVerification")
            .WithDisplayName("WhatsApp Webhook Verification");

        group.MapPost(string.Empty, deliveryHandler)
            .WithName("WhatsAppWebhookDelivery")
            .WithDisplayName("WhatsApp Webhook Delivery");

        return group;
    }
}
