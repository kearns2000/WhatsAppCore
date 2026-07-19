using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WhatsApp.Core.Client;
using WhatsApp.Core.Configuration;
using WhatsApp.Core.DependencyInjection;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Tests.Helpers;

internal static class WhatsAppClientTestHost
{
    public static (IServiceProvider Provider, RecordingHttpMessageHandler Handler) Create(
        Action<WhatsAppOptions>? configure = null,
        string accountName = WhatsAppOptions.DefaultAccountName,
        Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
    {
        var handler = new RecordingHttpMessageHandler(responder);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWhatsAppCore(accountName, options =>
        {
            options.PhoneNumberId = "123456789";
            options.AccessToken = "test-access-token";
            options.GraphApiVersion = "v21.0";
            options.BaseAddress = new Uri("https://graph.test.local/");
            options.AllowInsecureHttp = false;
            configure?.Invoke(options);
        });

        services.AddHttpClient(WhatsAppHttpClientNames.For(accountName))
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        return (provider, handler);
    }

    public static IWhatsAppClient GetClient(IServiceProvider provider, string accountName = WhatsAppOptions.DefaultAccountName) =>
        provider.GetRequiredService<IWhatsAppClientFactory>().CreateClient(accountName);
}
