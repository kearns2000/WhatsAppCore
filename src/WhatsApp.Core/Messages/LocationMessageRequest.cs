using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;
using WhatsApp.Core.Internal;

namespace WhatsApp.Core.Messages;

/// <summary>
/// A pin-drop location message.
/// </summary>
public sealed record LocationMessageRequest : WhatsAppMessageRequest
{
    /// <summary>
    /// Gets the latitude of the location, in degrees. Must be between
    /// <see cref="WhatsAppLimits.MinLatitude"/> and <see cref="WhatsAppLimits.MaxLatitude"/>.
    /// </summary>
    public required double Latitude { get; init; }

    /// <summary>
    /// Gets the longitude of the location, in degrees. Must be between
    /// <see cref="WhatsAppLimits.MinLongitude"/> and <see cref="WhatsAppLimits.MaxLongitude"/>.
    /// </summary>
    public required double Longitude { get; init; }

    /// <summary>
    /// Gets the optional display name of the location (e.g. a venue name).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the optional display address of the location.
    /// </summary>
    public string? Address { get; init; }

    internal override string Type => "location";

    internal override void Validate()
    {
        if (double.IsNaN(Latitude) || Latitude < WhatsAppLimits.MinLatitude || Latitude > WhatsAppLimits.MaxLatitude)
        {
            throw new WhatsAppValidationException(
                $"Latitude must be between {WhatsAppLimits.MinLatitude} and {WhatsAppLimits.MaxLatitude}.");
        }

        if (double.IsNaN(Longitude) || Longitude < WhatsAppLimits.MinLongitude || Longitude > WhatsAppLimits.MaxLongitude)
        {
            throw new WhatsAppValidationException(
                $"Longitude must be between {WhatsAppLimits.MinLongitude} and {WhatsAppLimits.MaxLongitude}.");
        }
    }

    internal override JsonNode BuildTypePayload()
    {
        var obj = new JsonObject
        {
            ["latitude"] = Latitude,
            ["longitude"] = Longitude,
        };

        if (!string.IsNullOrEmpty(Name))
        {
            obj["name"] = Name;
        }

        if (!string.IsNullOrEmpty(Address))
        {
            obj["address"] = Address;
        }

        return obj;
    }
}
