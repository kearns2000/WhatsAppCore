using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsApp.Core.Configuration;

namespace WhatsApp.Core.Client;

/// <summary>
/// Default <see cref="IWhatsAppClientFactory"/> implementation.
/// </summary>
internal sealed class WhatsAppClientFactory(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<WhatsAppOptions> optionsMonitor,
    ILoggerFactory loggerFactory,
    TimeProvider timeProvider) : IWhatsAppClientFactory
{
    /// <inheritdoc />
    public IWhatsAppClient CreateClient(string accountName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        return new WhatsAppClient(
            accountName,
            httpClientFactory,
            optionsMonitor,
            loggerFactory.CreateLogger<WhatsAppClient>(),
            timeProvider);
    }
}
