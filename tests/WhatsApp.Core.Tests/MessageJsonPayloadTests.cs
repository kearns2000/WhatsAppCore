using System.Text.Json.Nodes;
using WhatsApp.Core.Errors;
using WhatsApp.Core.Messages;
using WhatsApp.Core.Tests.Helpers;

namespace WhatsApp.Core.Tests;

public sealed class MessageJsonPayloadTests
{
    [Fact]
    public void TextMessage_ProducesExpectedJson()
    {
        var request = new TextMessageRequest
        {
            To = "+15551234567",
            Body = "Hello world",
            PreviewUrl = true,
            Context = new WhatsAppReplyContext { MessageId = "wamid.context" },
        };

        JsonTestHelpers.AssertJsonEqual(
            """
            {
              "messaging_product": "whatsapp",
              "recipient_type": "individual",
              "to": "15551234567",
              "type": "text",
              "text": {
                "body": "Hello world",
                "preview_url": true
              },
              "context": {
                "message_id": "wamid.context"
              }
            }
            """,
            request.ToJsonPayload());
    }

    [Fact]
    public void TextMessage_IncludesPreviewUrlFalseByDefault()
    {
        var payload = new TextMessageRequest { To = "15551234567", Body = "Hi" }.ToJsonPayload();
        Assert.Equal(false, payload["text"]!["preview_url"]!.GetValue<bool>());
    }

    [Fact]
    public void TemplateMessage_UsesSnakeCaseAndOmitsEmptyComponents()
    {
        var request = new TemplateMessageRequest
        {
            To = "15551234567",
            Name = "hello_world",
            LanguageCode = "en_US",
            Components =
            [
                new WhatsAppTemplateComponent
                {
                    ComponentType = "body",
                    Parameters = [WhatsAppTemplateParameter.ForText("Patrick")],
                },
            ],
        };

        JsonTestHelpers.AssertJsonEqual(
            """
            {
              "messaging_product": "whatsapp",
              "recipient_type": "individual",
              "to": "15551234567",
              "type": "template",
              "template": {
                "name": "hello_world",
                "language": { "code": "en_US" },
                "components": [
                  {
                    "type": "body",
                    "parameters": [
                      { "type": "text", "text": "Patrick" }
                    ]
                  }
                ]
              }
            }
            """,
            request.ToJsonPayload());
    }

    [Fact]
    public void ImageMessage_WithMediaId_OmitsNullCaption()
    {
        var payload = new ImageMessageRequest { To = "15551234567", MediaId = "media-123" }.ToJsonPayload();
        Assert.Equal("media-123", payload["image"]!["id"]!.GetValue<string>());
        Assert.False(payload["image"]!.AsObject().ContainsKey("caption"));
    }

    [Fact]
    public void ImageMessage_WithLink_IncludesCaption()
    {
        var payload = new ImageMessageRequest
        {
            To = "15551234567",
            Link = "https://example.com/image.png",
            Caption = "Look",
        }.ToJsonPayload();

        Assert.Equal("https://example.com/image.png", payload["image"]!["link"]!.GetValue<string>());
        Assert.Equal("Look", payload["image"]!["caption"]!.GetValue<string>());
    }

    [Fact]
    public void DocumentMessage_IncludesFileName()
    {
        var payload = new DocumentMessageRequest
        {
            To = "15551234567",
            MediaId = "doc-1",
            FileName = "invoice.pdf",
        }.ToJsonPayload();

        Assert.Equal("invoice.pdf", payload["document"]!["filename"]!.GetValue<string>());
    }

    [Fact]
    public void LocationMessage_OmitsOptionalNullFields()
    {
        var payload = new LocationMessageRequest
        {
            To = "15551234567",
            Latitude = 37.7749,
            Longitude = -122.4194,
        }.ToJsonPayload();

        Assert.False(payload["location"]!.AsObject().ContainsKey("name"));
        Assert.False(payload["location"]!.AsObject().ContainsKey("address"));
    }

    [Fact]
    public void InteractiveButtonsMessage_ProducesExpectedJson()
    {
        var request = new InteractiveMessageRequest
        {
            To = "15551234567",
            Body = new WhatsAppInteractiveBody { Text = "Choose" },
            Action = new WhatsAppInteractiveButtonsAction
            {
                Buttons =
                [
                    new WhatsAppInteractiveButton { Id = "yes", Title = "Yes" },
                    new WhatsAppInteractiveButton { Id = "no", Title = "No" },
                ],
            },
        };

        JsonTestHelpers.AssertJsonEqual(
            """
            {
              "messaging_product": "whatsapp",
              "recipient_type": "individual",
              "to": "15551234567",
              "type": "interactive",
              "interactive": {
                "type": "button",
                "body": { "text": "Choose" },
                "action": {
                  "buttons": [
                    {
                      "type": "reply",
                      "reply": { "id": "yes", "title": "Yes" }
                    },
                    {
                      "type": "reply",
                      "reply": { "id": "no", "title": "No" }
                    }
                  ]
                }
              }
            }
            """,
            request.ToJsonPayload());
    }

    [Fact]
    public void ReactionMessage_UsesEmptyEmojiToRemove()
    {
        var payload = new ReactionMessageRequest
        {
            To = "15551234567",
            MessageId = "wamid.abc",
            Emoji = null,
        }.ToJsonPayload();

        Assert.Equal(string.Empty, payload["reaction"]!["emoji"]!.GetValue<string>());
    }

    [Fact]
    public void ContactsMessage_ProducesExpectedJson()
    {
        var request = new ContactsMessageRequest
        {
            To = "15551234567",
            Contacts =
            [
                new WhatsAppContact
                {
                    Name = new WhatsAppContactName { FormattedName = "Jane Doe", FirstName = "Jane", LastName = "Doe" },
                    Phones = [new WhatsAppContactPhone { Phone = "+1 555-0100", Type = "CELL" }],
                },
            ],
        };

        var payload = request.ToJsonPayload();
        Assert.Equal("contacts", payload["type"]!.GetValue<string>());
        Assert.Equal("Jane Doe", payload["contacts"]![0]!["name"]!["formatted_name"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("155-5123-4567")]
    [InlineData("(155) 51234567")]
    [InlineData("")]
    public void RecipientValidation_RejectsInvalidNumbers(string to)
    {
        var request = new TextMessageRequest { To = to, Body = "Hi" };
        Assert.Throws<WhatsAppValidationException>(() => request.ToJsonPayload());
    }

    [Fact]
    public void RecipientValidation_NormalizesLeadingPlus()
    {
        var payload = new TextMessageRequest { To = "+15551234567", Body = "Hi" }.ToJsonPayload();
        Assert.Equal("15551234567", payload["to"]!.GetValue<string>());
    }
}
