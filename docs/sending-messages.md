# Sending Messages

WhatsApp.Core provides strongly typed request models and convenience extension methods for every supported outbound message type. All paths go through `IWhatsAppClient.SendMessageAsync` - convenience methods construct the appropriate request record.

## Recipient format

Recipients must be E.164-style phone numbers. An optional leading `+` is accepted and stripped before sending. Formatting characters (spaces, dashes, parentheses) are rejected.

```csharp
await client.SendTextAsync("353871234567", "Hello", stopToken: stopToken);
await client.SendTextAsync("+353871234567", "Hello", stopToken: stopToken); // equivalent
```

The library does not perform network-based phone number validation.

## Text messages

```csharp
var response = await client.SendTextAsync(
    to: "353871234567",
    body: "Hello from WhatsApp.Core",
    previewUrl: false,
    context: null,
    stopToken: stopToken);
```

| Parameter | Description |
|-----------|-------------|
| `to` | Recipient phone number |
| `body` | Message text |
| `previewUrl` | Render a link preview for the first URL in the body |
| `context` | Optional reply context (`WhatsAppReplyContext`) |

## Template messages

Templates must be pre-approved in your WhatsApp Business Account:

```csharp
await client.SendTemplateAsync(
    to: "353871234567",
    templateName: "order_confirmation",
    languageCode: "en_US",
    components:
    [
        new WhatsAppTemplateComponent
        {
            ComponentType = "body",
            Parameters =
            [
                WhatsAppTemplateParameter.ForText("ORD-12345"),
                WhatsAppTemplateParameter.ForText("€49.99"),
            ],
        },
    ],
    stopToken: stopToken);
```

Template components support header, body, and button parameter types via `WhatsAppTemplateParameter.ForText`, `ForCurrency`, `ForDateTime`, and `ForImage`.

## Image messages

Send by uploaded media id or public URL (exactly one required):

```csharp
// By media id (after UploadMediaAsync)
await client.SendImageAsync(
    to: "353871234567",
    mediaId: upload.Id,
    caption: "Your receipt",
    stopToken: stopToken);

// By public link
await client.SendImageAsync(
    to: "353871234567",
    link: "https://example.com/image.jpg",
    caption: "Sample image",
    stopToken: stopToken);
```

## Document messages

```csharp
await client.SendDocumentAsync(
    to: "353871234567",
    mediaId: upload.Id,
    caption: "Your invoice",
    fileName: "invoice.pdf",
    stopToken: stopToken);
```

## Audio, video, and sticker messages

```csharp
await client.SendAudioAsync(to: "353...", mediaId: id, stopToken: stopToken);
await client.SendVideoAsync(to: "353...", link: url, caption: "Demo", stopToken: stopToken);
await client.SendStickerAsync(to: "353...", mediaId: id, stopToken: stopToken);
```

## Location messages

```csharp
await client.SendLocationAsync(
    to: "353871234567",
    latitude: 53.3498,
    longitude: -6.2603,
    name: "Dublin",
    address: "Ireland",
    stopToken: stopToken);
```

Latitude must be between -90 and 90; longitude between -180 and 180.

## Contact cards

```csharp
await client.SendContactsAsync(
    to: "353871234567",
    contacts:
    [
        new WhatsAppContact
        {
            Name = new WhatsAppContactName { FormattedName = "Jane Doe" },
            Phones = [new WhatsAppContactPhone { Phone = "+353871234567", Type = "CELL" }],
        },
    ],
    stopToken: stopToken);
```

## Interactive messages

Send reply buttons, lists, or call-to-action URL buttons:

```csharp
await client.SendInteractiveAsync(
    to: "353871234567",
    action: new WhatsAppInteractiveAction
    {
        ActionType = "button",
        Buttons =
        [
            new WhatsAppInteractiveButton { Id = "yes", Title = "Yes" },
            new WhatsAppInteractiveButton { Id = "no", Title = "No" },
        ],
    },
    bodyText: "Would you like to continue?",
    header: new WhatsAppInteractiveHeader { HeaderType = "text", Text = "Confirmation" },
    footer: new WhatsAppInteractiveFooter { Text = "Reply within 24 hours" },
    stopToken: stopToken);
```

Inbound replies to interactive messages arrive as `WhatsAppInteractiveReplyMessageEvent` webhooks.

## Reactions

React to a previously received message:

```csharp
await client.SendReactionAsync(
    to: "353871234567",
    messageId: "wamid.HBg...",
    emoji: "👍",
    stopToken: stopToken);
```

Remove a reaction by passing `null` or an empty emoji:

```csharp
await client.SendReactionAsync(to: "353...", messageId: "wamid...", emoji: null, stopToken: stopToken);
```

## Reply context

Reply to a specific inbound message:

```csharp
var context = new WhatsAppReplyContext { MessageId = "wamid.HBg..." };

await client.SendTextAsync(
    to: "353871234567",
    body: "Got it!",
    context: context,
    stopToken: stopToken);
```

## Read receipts

Mark an inbound message as read (shows blue check marks to the sender):

```csharp
await client.MarkMessageAsReadAsync("wamid.HBg...", stopToken);
```

## Raw message escape hatch

For message types not yet modeled by the library:

```csharp
using System.Text.Json.Nodes;

var payload = new JsonObject
{
    ["to"] = "353871234567",
    ["type"] = "text",
    ["text"] = new JsonObject { ["body"] = "Hello" },
};

await client.SendRawMessageAsync(payload, stopToken);
```

The payload is always posted to this client's configured `/messages` endpoint. Fields cannot redirect the request to another host or override authentication.

## Response handling

All send methods return `SendMessageResponse`:

```csharp
SendMessageResponse response = await client.SendTextAsync(...);

string? messageId = response.Messages.FirstOrDefault()?.Id;
string? requestId = response.Metadata.RequestId;
```

Use the returned `messageId` (`wamid...`) to correlate with delivery status webhooks.

## Client-side validation

These conditions throw `WhatsAppValidationException` before any HTTP call:

- Missing or empty recipient
- Empty text body
- Missing template name or language
- Both `mediaId` and `link` set (or neither set) for media messages
- Missing reaction target message id
- Invalid latitude or longitude
- Empty interactive button/list sections

## Related documentation

- [Media](media.md) - Upload media before sending by id
- [Error handling](error-handling.md) - API errors and retry guidance
- [Architecture](architecture.md) - Why message sends are not retried
