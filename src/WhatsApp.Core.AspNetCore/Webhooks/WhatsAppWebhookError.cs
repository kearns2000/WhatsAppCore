namespace WhatsApp.Core.AspNetCore.Webhooks;

/// <summary>
/// An error Meta reported against a specific inbound message or outbound message status
/// update (distinct from <see cref="WhatsApp.Core.Errors.WhatsAppApiException"/>, which
/// represents a failure of an outbound API call made by this application).
/// </summary>
public sealed record WhatsAppWebhookError
{
    /// <summary>
    /// Gets the Graph API error code, if reported.
    /// </summary>
    public int? Code { get; init; }

    /// <summary>
    /// Gets a short error title, if reported.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets a human-readable error message, if reported.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets additional error details, if reported.
    /// </summary>
    public string? Details { get; init; }
}
