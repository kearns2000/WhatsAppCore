using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using WhatsApp.Core.Configuration;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Client;

/// <summary>
/// A <see cref="DelegatingHandler"/> that attaches the configured bearer access token to
/// outgoing requests whose host is an allowed Graph/media host for a given account. Resolves
/// the token fresh from <see cref="IOptionsMonitor{TOptions}"/> on every request, so credential
/// rotations (e.g. via configuration reload) take effect without recreating the
/// <see cref="HttpClient"/>.
/// </summary>
internal sealed class WhatsAppAuthenticationHandler(string accountName, IOptionsMonitor<WhatsAppOptions> optionsMonitor)
    : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var options = optionsMonitor.Get(accountName);
        var host = request.RequestUri?.IdnHost;
        if (MediaDownloadUrlValidator.IsAllowedCredentialedHost(host, options))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
