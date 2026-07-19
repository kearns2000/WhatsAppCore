namespace WhatsApp.Core.Errors;

/// <summary>
/// Thrown when a request constructed by the caller fails client-side validation before any
/// network call is made, for example a missing recipient, an empty message body, or an
/// interactive message with no content. Because these failures never reach the network, they
/// are always safe to fix and retry immediately.
/// </summary>
public sealed class WhatsAppValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppValidationException"/> class.
    /// </summary>
    /// <param name="message">A message describing why validation failed.</param>
    public WhatsAppValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WhatsAppValidationException"/> class.
    /// </summary>
    /// <param name="message">A message describing why validation failed.</param>
    /// <param name="innerException">The exception that caused this validation failure.</param>
    public WhatsAppValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
