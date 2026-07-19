using WhatsApp.Core.Errors;

namespace WhatsApp.Core.Internal;

/// <summary>
/// Validates and normalizes phone numbers accepted by the WhatsApp Cloud API. Numbers must be
/// expressible in E.164 form: an optional leading '+' followed only by digits. No network calls
/// or number-plan lookups are performed; this is a purely syntactic check.
/// </summary>
internal static class PhoneNumberValidator
{
    /// <summary>
    /// Validates and normalizes a recipient (or contact) phone number, stripping any leading
    /// '+' sign. Formatting characters such as spaces, dashes, or parentheses are rejected
    /// rather than silently stripped, so that callers are alerted to malformed input.
    /// </summary>
    /// <param name="value">The raw phone number supplied by the caller.</param>
    /// <param name="parameterName">The name of the parameter or field being validated, used in error messages.</param>
    /// <returns>The normalized phone number, containing only digits.</returns>
    /// <exception cref="WhatsAppValidationException">The value is empty or not a valid E.164 number.</exception>
    public static string NormalizeRecipient(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new WhatsAppValidationException($"'{parameterName}' must not be empty.");
        }

        var trimmed = value.Trim();
        var digits = trimmed.StartsWith('+') ? trimmed[1..] : trimmed;

        if (digits.Length == 0)
        {
            throw new WhatsAppValidationException($"'{parameterName}' must contain at least one digit.");
        }

        foreach (var c in digits)
        {
            if (!char.IsAsciiDigit(c))
            {
                throw new WhatsAppValidationException(
                    $"'{parameterName}' must be an E.164 phone number containing only digits and an optional leading '+'. " +
                    "Formatting characters such as spaces, dashes, or parentheses are not allowed.");
            }
        }

        if (digits.Length < WhatsAppLimits.MinPhoneNumberDigits || digits.Length > WhatsAppLimits.MaxPhoneNumberDigits)
        {
            throw new WhatsAppValidationException(
                $"'{parameterName}' must contain between {WhatsAppLimits.MinPhoneNumberDigits} and {WhatsAppLimits.MaxPhoneNumberDigits} digits.");
        }

        return digits;
    }
}
