using WhatsApp.Core.Configuration;
using WhatsApp.Core.Errors;

namespace WhatsApp.Core.Internal;

/// <summary>
/// Ensures media download URLs returned by the Graph API point at HTTPS Meta (or configured)
/// hosts before the client follows them with a bearer token, mitigating SSRF and credential
/// exfiltration if a URL is unexpected.
/// </summary>
internal static class MediaDownloadUrlValidator
{
    public static void EnsureAllowed(string? url, WhatsAppOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var absoluteUrl))
        {
            throw new WhatsAppValidationException("The media download URL is missing or is not an absolute URI.");
        }

        if (!string.Equals(absoluteUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new WhatsAppValidationException("The media download URL must use HTTPS.");
        }

        var host = absoluteUrl.IdnHost;
        if (IsAllowedHost(host, options))
        {
            return;
        }

        throw new WhatsAppValidationException(
            $"The media download URL host '{host}' is not an allowed Meta media host.");
    }

    private static bool IsAllowedHost(string host, WhatsAppOptions options)
    {
        if (string.Equals(host, options.BaseAddress.IdnHost, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (host.Equals("lookaside.fbsbx.com", StringComparison.OrdinalIgnoreCase)
            || host.Equals("graph.facebook.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".fbsbx.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".fbcdn.net", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".whatsapp.net", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".facebook.com", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var allowed in options.AllowedMediaDownloadHosts)
        {
            if (string.IsNullOrWhiteSpace(allowed))
            {
                continue;
            }

            if (string.Equals(host, allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (allowed.StartsWith('.')
                && host.EndsWith(allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
