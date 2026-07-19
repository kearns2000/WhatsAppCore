using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;

namespace WhatsApp.Core.Messages;

/// <summary>
/// A message sharing one or more contact cards.
/// </summary>
public sealed record ContactsMessageRequest : WhatsAppMessageRequest
{
    /// <summary>
    /// Gets the contact cards to share. Must contain at least one entry.
    /// </summary>
    public required IReadOnlyList<WhatsAppContact> Contacts { get; init; }

    internal override string Type => "contacts";

    internal override void Validate()
    {
        if (Contacts is null or { Count: 0 })
        {
            throw new WhatsAppValidationException("A contacts message must include at least one contact.");
        }

        foreach (var contact in Contacts)
        {
            contact.Validate();
        }
    }

    internal override JsonNode BuildTypePayload()
    {
        var array = new JsonArray();
        foreach (var contact in Contacts)
        {
            array.Add(contact.ToJson());
        }

        return array;
    }
}
