using WhatsApp.Core.Internal;
using WhatsApp.Core.Serialization;
using WhatsApp.Core.Serialization.Wire;

namespace WhatsApp.Core.Errors;

/// <summary>
/// Builds a <see cref="WhatsAppApiException"/> from a non-success HTTP response returned by the
/// Graph API, parsing the structured error body when present.
/// </summary>
internal static class WhatsAppErrorParser
{
    /// <summary>
    /// Creates the exception to throw for a non-success HTTP response. Never throws itself;
    /// parsing failures are swallowed and result in a best-effort exception describing the raw
    /// status code.
    /// </summary>
    /// <param name="response">The non-success HTTP response.</param>
    /// <param name="cancellationToken">A token used to cancel reading the response body.</param>
    /// <returns>The exception describing the failure.</returns>
    public static async Task<WhatsAppApiException> CreateExceptionAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        GraphErrorDetailWire? error = null;

        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var body = await System.Text.Json.JsonSerializer
                .DeserializeAsync<GraphErrorWireResponse>(stream, WhatsAppJsonSerializerOptions.Default, cancellationToken)
                .ConfigureAwait(false);
            error = body?.Error;
        }
        catch (System.Text.Json.JsonException)
        {
            // The body was not a structured Graph API error (e.g. an HTML error page from an
            // intermediate proxy). Fall back to a status-code-only message below.
        }
        catch (IOException)
        {
            // The body could not be read at all (e.g. the connection was reset).
        }

        var message = error?.Message
            ?? $"The WhatsApp Cloud API request failed with status code {(int)response.StatusCode} ({response.StatusCode}).";
        var retryAfter = HttpRetryAfterParser.Parse(response.Headers, DateTimeOffset.UtcNow);
        var isTransient = GraphErrorClassifier.IsTransient(response.StatusCode, error?.Code, error?.ErrorSubcode);

        return new WhatsAppApiException(
            message,
            response.StatusCode,
            error?.Code,
            error?.ErrorSubcode,
            error?.Type,
            error?.FbTraceId,
            isTransient,
            retryAfter,
            error?.ErrorData);
    }
}
