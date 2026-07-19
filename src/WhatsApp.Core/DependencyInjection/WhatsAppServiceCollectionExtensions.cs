using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using WhatsApp.Core.Client;
using WhatsApp.Core.Configuration;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.DependencyInjection;

/// <summary>
/// Extension methods for registering WhatsApp Cloud API clients with an
/// <see cref="IServiceCollection"/>.
/// </summary>
public static class WhatsAppServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default WhatsApp account, configured from a configuration section (e.g.
    /// <c>builder.Configuration.GetSection("WhatsApp")</c>), and registers a default
    /// <see cref="IWhatsAppClient"/> resolvable without a name.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section to bind <see cref="WhatsAppOptions"/> from.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddWhatsAppCore(
        this IServiceCollection services, IConfigurationSection configurationSection) =>
        services.AddWhatsAppCore(WhatsAppOptions.DefaultAccountName, configurationSection);

    /// <summary>
    /// Registers the default WhatsApp account, configured via a delegate, and registers a
    /// default <see cref="IWhatsAppClient"/> resolvable without a name.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">A delegate that populates the account's <see cref="WhatsAppOptions"/>.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddWhatsAppCore(
        this IServiceCollection services, Action<WhatsAppOptions> configure) =>
        services.AddWhatsAppCore(WhatsAppOptions.DefaultAccountName, configure);

    /// <summary>
    /// Registers a named WhatsApp account, configured via a delegate. Use
    /// <see cref="IWhatsAppClientFactory"/> to resolve a client for this account by name.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="accountName">The logical name to register this account under.</param>
    /// <param name="configure">A delegate that populates the account's <see cref="WhatsAppOptions"/>.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddWhatsAppCore(
        this IServiceCollection services, string accountName, Action<WhatsAppOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        ArgumentNullException.ThrowIfNull(configure);

        AddCoreServices(services);
        services.AddOptions<WhatsAppOptions>(accountName).Configure(configure).ValidateOnStart();
        RegisterAccount(services, accountName);
        return services;
    }

    /// <summary>
    /// Registers a named WhatsApp account, configured from a configuration section. Use
    /// <see cref="IWhatsAppClientFactory"/> to resolve a client for this account by name.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="accountName">The logical name to register this account under.</param>
    /// <param name="configurationSection">The configuration section to bind <see cref="WhatsAppOptions"/> from.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddWhatsAppCore(
        this IServiceCollection services, string accountName, IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        ArgumentNullException.ThrowIfNull(configurationSection);

        AddCoreServices(services);
        services.AddOptions<WhatsAppOptions>(accountName).Bind(configurationSection).ValidateOnStart();
        RegisterAccount(services, accountName);
        return services;
    }

    private static void AddCoreServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<WhatsAppOptions>, WhatsAppOptionsValidator>());
        services.TryAddSingleton<IWhatsAppClientFactory, WhatsAppClientFactory>();
    }

    private static void RegisterAccount(IServiceCollection services, string accountName)
    {
        var httpClientName = WhatsAppHttpClientNames.For(accountName);

        services.AddHttpClient(httpClientName, (provider, httpClient) =>
            {
                var options = provider.GetRequiredService<IOptionsMonitor<WhatsAppOptions>>().Get(accountName);
                httpClient.BaseAddress = options.BaseAddress;
                httpClient.Timeout = options.Timeout;
            })
            .ConfigurePrimaryHttpMessageHandler(static () => new SocketsHttpHandler
            {
                // Media downloads re-validate each Location; Graph calls typically do not redirect.
                AllowAutoRedirect = false,
                AutomaticDecompression = System.Net.DecompressionMethods.All,
            })
            .AddHttpMessageHandler(provider =>
                new WhatsAppAuthenticationHandler(accountName, provider.GetRequiredService<IOptionsMonitor<WhatsAppOptions>>()));

        if (string.Equals(accountName, WhatsAppOptions.DefaultAccountName, StringComparison.Ordinal))
        {
            services.TryAddSingleton<IWhatsAppClient>(provider =>
                provider.GetRequiredService<IWhatsAppClientFactory>().CreateClient(WhatsAppOptions.DefaultAccountName));
        }
    }
}
