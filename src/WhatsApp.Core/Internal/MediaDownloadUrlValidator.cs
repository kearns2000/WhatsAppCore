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

        EnsureAllowed(absoluteUrl, options);
    }

    public static void EnsureAllowed(Uri url, WhatsAppOptions options)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(options);

        if (!url.IsAbsoluteUri)
        {
            throw new WhatsAppValidationException("The media download URL is missing or is not an absolute URI.");
        }

        if (!string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new WhatsAppValidationException("The media download URL must use HTTPS.");
        }

        if (IsAllowedCredentialedHost(url.IdnHost, options))
        {
            return;
        }

        throw new WhatsAppValidationException(
            $"The media download URL host '{url.IdnHost}' is not an allowed Meta media host.");
    }

    /// <summary>
    /// Returns whether a request host may receive the Graph bearer access token.
    /// </summary>
    public static bool IsAllowedCredentialedHost(string? host, WhatsAppOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        if (string.Equals(host, options.BaseAddress.IdnHost, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Tight allowlist: exact Graph host, Lookaside CDN, and well-known Meta media suffixes.
        // Intentionally excludes a broad "*.facebook.com" match.
        if (host.Equals("lookaside.fbsbx.com", StringComparison.OrdinalIgnoreCase)
            || host.Equals("graph.facebook.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".fbsbx.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".fbcdn.net", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".cdn.whatsapp.net", StringComparison.OrdinalIgnoreCase)
            || host.Equals("mmg.whatsapp.net", StringComparison.OrdinalIgnoreCase))
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
