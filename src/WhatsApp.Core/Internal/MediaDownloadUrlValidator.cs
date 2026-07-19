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
            if (!IsValidAllowedHostEntry(allowed, out _))
            {
                continue;
            }

            if (string.Equals(host, allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (allowed.StartsWith('.')
                && host.EndsWith(allowed, StringComparison.OrdinalIgnoreCase)
                && host.Length > allowed.Length)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Validates a configured <see cref="WhatsAppOptions.AllowedMediaDownloadHosts"/> entry.
    /// Suffix entries must be multi-label (e.g. <c>.fbcdn.net</c>), never a bare public suffix
    /// like <c>.com</c>.
    /// </summary>
    public static bool IsValidAllowedHostEntry(string? entry, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(entry))
        {
            error = $"{nameof(WhatsAppOptions.AllowedMediaDownloadHosts)} entries must be non-empty host names or multi-label suffixes.";
            return false;
        }

        if (entry.Contains('/', StringComparison.Ordinal)
            || entry.Contains(':', StringComparison.Ordinal)
            || entry.Contains(' ', StringComparison.Ordinal))
        {
            error = $"{nameof(WhatsAppOptions.AllowedMediaDownloadHosts)} entry '{entry}' must be a host name or DNS suffix, not a URL.";
            return false;
        }

        if (entry.StartsWith('.'))
        {
            // Require at least one additional label separator (".cdn.example" / ".fbcdn.net").
            if (entry.Length < 5 || entry.IndexOf('.', 1) <= 1 || entry.EndsWith('.'))
            {
                error = $"{nameof(WhatsAppOptions.AllowedMediaDownloadHosts)} suffix '{entry}' must be a multi-label DNS suffix (e.g. \".fbcdn.net\"), not a bare public suffix.";
                return false;
            }

            return true;
        }

        if (entry.Contains('.', StringComparison.Ordinal) && !entry.StartsWith('.') && !entry.EndsWith('.'))
        {
            return true;
        }

        // Single-label exact hosts (e.g. "localhost") are allowed for tests.
        if (!entry.Contains('.', StringComparison.Ordinal)
            && entry.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-'))
        {
            return true;
        }

        error = $"{nameof(WhatsAppOptions.AllowedMediaDownloadHosts)} entry '{entry}' is not a valid host name or multi-label suffix.";
        return false;
    }
}
