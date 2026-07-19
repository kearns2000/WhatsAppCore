using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WhatsApp.Core.Testing.Builders;

/// <summary>
/// Fluent builder for Meta WhatsApp Cloud API webhook JSON payloads used in tests.
/// </summary>
public sealed class WhatsAppWebhookBuilder
{
    private string _businessAccountId = "WABA_ID";
    private string _phoneNumberId = "PHONE_NUMBER_ID";
    private string _displayPhoneNumber = "15550001111";
    private readonly List<JsonObject> _messages = [];
    private readonly List<JsonObject> _statuses = [];
    private readonly List<JsonObject> _contacts = [];

    private WhatsAppWebhookBuilder()
    {
    }

    /// <summary>Creates a new webhook builder.</summary>
    public static WhatsAppWebhookBuilder Create() => new();

    /// <summary>Sets the WhatsApp Business Account id.</summary>
    public WhatsAppWebhookBuilder WithBusinessAccountId(string id)
    {
        _businessAccountId = id;
        return this;
    }

    /// <summary>Sets the phone number id.</summary>
    public WhatsAppWebhookBuilder WithPhoneNumberId(string id)
    {
        _phoneNumberId = id;
        return this;
    }

    /// <summary>Sets the display phone number.</summary>
    public WhatsAppWebhookBuilder WithDisplayPhoneNumber(string displayPhoneNumber)
    {
        _displayPhoneNumber = displayPhoneNumber;
        return this;
    }

    /// <summary>Adds an inbound text message.</summary>
    public WhatsAppWebhookBuilder AddTextMessage(
        string messageId,
        string from,
        string body,
        long? unixTimestamp = null)
    {
        EnsureContact(from);
        _messages.Add(new JsonObject
        {
            ["from"] = from,
            ["id"] = messageId,
            ["timestamp"] = (unixTimestamp ?? 1700000000).ToString(CultureInfo.InvariantCulture),
            ["type"] = "text",
            ["text"] = new JsonObject { ["body"] = body },
        });
        return this;
    }

    /// <summary>Adds an inbound message with an arbitrary type and payload fragment.</summary>
    public WhatsAppWebhookBuilder AddMessage(JsonObject message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.TryGetPropertyValue("from", out var fromNode) && fromNode is JsonValue fromValue &&
            fromValue.TryGetValue<string>(out var from))
        {
            EnsureContact(from);
        }

        _messages.Add(message);
        return this;
    }

    /// <summary>Adds a delivered status update.</summary>
    public WhatsAppWebhookBuilder AddDeliveredStatus(
        string messageId,
        string recipientId,
        long? unixTimestamp = null) =>
        AddStatus(messageId, recipientId, "delivered", unixTimestamp);

    /// <summary>Adds a sent status update.</summary>
    public WhatsAppWebhookBuilder AddSentStatus(
        string messageId,
        string recipientId,
        long? unixTimestamp = null) =>
        AddStatus(messageId, recipientId, "sent", unixTimestamp);

    /// <summary>Adds a read status update.</summary>
    public WhatsAppWebhookBuilder AddReadStatus(
        string messageId,
        string recipientId,
        long? unixTimestamp = null) =>
        AddStatus(messageId, recipientId, "read", unixTimestamp);

    /// <summary>Adds a failed status update.</summary>
    public WhatsAppWebhookBuilder AddFailedStatus(
        string messageId,
        string recipientId,
        int errorCode = 131026,
        string errorTitle = "Message undeliverable",
        long? unixTimestamp = null)
    {
        var status = CreateStatus(messageId, recipientId, "failed", unixTimestamp);
        status["errors"] = new JsonArray
        {
            new JsonObject
            {
                ["code"] = errorCode,
                ["title"] = errorTitle,
                ["message"] = errorTitle,
            },
        };
        _statuses.Add(status);
        return this;
    }

    /// <summary>Adds a status with an arbitrary status string.</summary>
    public WhatsAppWebhookBuilder AddStatus(
        string messageId,
        string recipientId,
        string status,
        long? unixTimestamp = null)
    {
        _statuses.Add(CreateStatus(messageId, recipientId, status, unixTimestamp));
        return this;
    }

    /// <summary>Builds the webhook as a <see cref="JsonObject"/>.</summary>
    public JsonObject Build()
    {
        var value = new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["metadata"] = new JsonObject
            {
                ["display_phone_number"] = _displayPhoneNumber,
                ["phone_number_id"] = _phoneNumberId,
            },
        };

        if (_contacts.Count > 0)
        {
            value["contacts"] = new JsonArray(_contacts.Select(static c => c.DeepClone()).ToArray());
        }

        if (_messages.Count > 0)
        {
            value["messages"] = new JsonArray(_messages.Select(static m => m.DeepClone()).ToArray());
        }

        if (_statuses.Count > 0)
        {
            value["statuses"] = new JsonArray(_statuses.Select(static s => s.DeepClone()).ToArray());
        }

        return new JsonObject
        {
            ["object"] = "whatsapp_business_account",
            ["entry"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = _businessAccountId,
                    ["changes"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["field"] = "messages",
                            ["value"] = value,
                        },
                    },
                },
            },
        };
    }

    /// <summary>Builds the webhook JSON as a compact string.</summary>
    public string BuildJson() => Build().ToJsonString(new JsonSerializerOptions { WriteIndented = false });

    /// <summary>Builds the webhook JSON as UTF-8 bytes.</summary>
    public byte[] BuildBytes() => Encoding.UTF8.GetBytes(BuildJson());

    private void EnsureContact(string waId)
    {
        if (_contacts.Any(c => c["wa_id"]?.GetValue<string>() == waId))
        {
            return;
        }

        _contacts.Add(new JsonObject
        {
            ["profile"] = new JsonObject { ["name"] = "Test User" },
            ["wa_id"] = waId,
        });
    }

    private static JsonObject CreateStatus(
        string messageId,
        string recipientId,
        string status,
        long? unixTimestamp) =>
        new()
        {
            ["id"] = messageId,
            ["status"] = status,
            ["timestamp"] = (unixTimestamp ?? 1700000000).ToString(CultureInfo.InvariantCulture),
            ["recipient_id"] = recipientId,
        };
}
