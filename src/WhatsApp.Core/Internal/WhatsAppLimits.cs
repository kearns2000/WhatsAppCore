namespace WhatsApp.Core.Internal;

/// <summary>
/// Protocol-level limits imposed by the Meta WhatsApp Cloud API, used for client-side validation
/// so that obviously invalid requests fail fast instead of round-tripping to the network.
/// </summary>
internal static class WhatsAppLimits
{
    /// <summary>The maximum length, in characters, of a free-form text message body.</summary>
    public const int MaxTextBodyLength = 4096;

    /// <summary>The maximum length, in characters, of an interactive body/footer/header text field.</summary>
    public const int MaxInteractiveTextLength = 1024;

    /// <summary>The maximum number of quick-reply buttons allowed on an interactive button message.</summary>
    public const int MaxInteractiveButtons = 3;

    /// <summary>The maximum number of sections allowed on an interactive list message.</summary>
    public const int MaxInteractiveListSections = 10;

    /// <summary>The maximum number of rows allowed across all sections of an interactive list message.</summary>
    public const int MaxInteractiveListRows = 10;

    /// <summary>The minimum number of digits in a normalized E.164 phone number.</summary>
    public const int MinPhoneNumberDigits = 7;

    /// <summary>The maximum number of digits in a normalized E.164 phone number.</summary>
    public const int MaxPhoneNumberDigits = 15;

    /// <summary>The minimum valid latitude value, in degrees.</summary>
    public const double MinLatitude = -90d;

    /// <summary>The maximum valid latitude value, in degrees.</summary>
    public const double MaxLatitude = 90d;

    /// <summary>The minimum valid longitude value, in degrees.</summary>
    public const double MinLongitude = -180d;

    /// <summary>The maximum valid longitude value, in degrees.</summary>
    public const double MaxLongitude = 180d;
}
