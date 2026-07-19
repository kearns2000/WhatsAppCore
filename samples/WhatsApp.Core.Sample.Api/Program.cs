using System.Diagnostics;
using WhatsApp.Core.AspNetCore.DependencyInjection;
using WhatsApp.Core.AspNetCore.Webhooks;
using WhatsApp.Core.Client;
using WhatsApp.Core.DependencyInjection;
using WhatsApp.Core.Errors;
using WhatsApp.Core.Messages;
using WhatsApp.Core.Responses;
using WhatsApp.Core.Sample.Api.Handlers;
using WhatsApp.Core.Sample.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddWhatsAppCore(builder.Configuration.GetSection("WhatsApp"));

builder.Services.AddWhatsAppWebhooks();
builder.Services
    .AddWhatsAppWebhookHandler<TextMessageHandler, WhatsAppTextMessageEvent>()
    .AddWhatsAppWebhookHandler<InteractiveReplyMessageHandler, WhatsAppInteractiveReplyMessageEvent>()
    .AddWhatsAppWebhookHandler<MessageDeliveredHandler, WhatsAppMessageDeliveredEvent>()
    .AddWhatsAppWebhookHandler<MessageFailedHandler, WhatsAppMessageFailedEvent>()
    .AddWhatsAppWebhookHandler<UnknownMessageHandler, UnknownWhatsAppMessageEvent>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var messages = app.MapGroup("/messages").WithTags("Messages");

// Outbound send endpoints proxy the WhatsApp access token. Bind them only in Development so a
// misconfigured public deployment cannot be used as an unauthenticated messaging relay.
if (app.Environment.IsDevelopment())
{
    messages.MapPost("/text", async (SendTextMessageRequest request, IWhatsAppClient client, CancellationToken stopToken) =>
    {
        try
        {
            var response = await client.SendTextAsync(
                to: request.To,
                body: request.Body,
                previewUrl: request.PreviewUrl,
                context: ToReplyContext(request.ReplyToMessageId),
                stopToken);

            return Results.Ok(ToResponse(response));
        }
        catch (Exception ex) when (ex is WhatsAppValidationException or WhatsAppApiException)
        {
            return ToErrorResult(ex);
        }
    });

    messages.MapPost("/template", async (SendTemplateMessageRequest request, IWhatsAppClient client, CancellationToken stopToken) =>
    {
        try
        {
            IReadOnlyList<WhatsAppTemplateComponent>? components = null;
            if (request.BodyParameters is { Count: > 0 })
            {
                components =
                [
                    new WhatsAppTemplateComponent
                    {
                        ComponentType = "body",
                        Parameters = request.BodyParameters
                            .Select(WhatsAppTemplateParameter.ForText)
                            .ToList(),
                    },
                ];
            }

            var response = await client.SendTemplateAsync(
                to: request.To,
                templateName: request.TemplateName,
                languageCode: request.LanguageCode,
                components: components,
                context: ToReplyContext(request.ReplyToMessageId),
                stopToken);

            return Results.Ok(ToResponse(response));
        }
        catch (Exception ex) when (ex is WhatsAppValidationException or WhatsAppApiException)
        {
            return ToErrorResult(ex);
        }
    });

    messages.MapPost("/image", async (SendImageMessageRequest request, IWhatsAppClient client, CancellationToken stopToken) =>
    {
        try
        {
            var response = await client.SendImageAsync(
                to: request.To,
                mediaId: request.MediaId,
                link: request.Link,
                caption: request.Caption,
                context: ToReplyContext(request.ReplyToMessageId),
                stopToken);

            return Results.Ok(ToResponse(response));
        }
        catch (Exception ex) when (ex is WhatsAppValidationException or WhatsAppApiException)
        {
            return ToErrorResult(ex);
        }
    });

    messages.MapPost("/document", async (SendDocumentMessageRequest request, IWhatsAppClient client, CancellationToken stopToken) =>
    {
        try
        {
            var response = await client.SendDocumentAsync(
                to: request.To,
                mediaId: request.MediaId,
                link: request.Link,
                caption: request.Caption,
                fileName: request.FileName,
                context: ToReplyContext(request.ReplyToMessageId),
                stopToken);

            return Results.Ok(ToResponse(response));
        }
        catch (Exception ex) when (ex is WhatsAppValidationException or WhatsAppApiException)
        {
            return ToErrorResult(ex);
        }
    });

    messages.MapPost("/reaction", async (SendReactionMessageRequest request, IWhatsAppClient client, CancellationToken stopToken) =>
    {
        try
        {
            var response = await client.SendReactionAsync(
                to: request.To,
                messageId: request.MessageId,
                emoji: request.Emoji,
                stopToken);

            return Results.Ok(ToResponse(response));
        }
        catch (Exception ex) when (ex is WhatsAppValidationException or WhatsAppApiException)
        {
            return ToErrorResult(ex);
        }
    });

    messages.MapPost("/{messageId}/read", async (string messageId, IWhatsAppClient client, CancellationToken stopToken) =>
    {
        try
        {
            await client.MarkMessageAsReadAsync(messageId, stopToken);
            return Results.NoContent();
        }
        catch (Exception ex) when (ex is WhatsAppValidationException or WhatsAppApiException)
        {
            return ToErrorResult(ex);
        }
    });
}

app.MapWhatsAppWebhook("/webhooks/whatsapp");

app.Run();

static WhatsAppReplyContext? ToReplyContext(string? replyToMessageId) =>
    string.IsNullOrWhiteSpace(replyToMessageId)
        ? null
        : new WhatsAppReplyContext { MessageId = replyToMessageId };

static object ToResponse(SendMessageResponse response) => new
{
    messageId = response.Messages.FirstOrDefault()?.Id,
    contacts = response.Contacts.Select(c => new { c.Input, c.WaId }),
    requestId = response.Metadata.RequestId,
};

static IResult ToErrorResult(Exception exception) => exception switch
{
    WhatsAppValidationException validation => Results.BadRequest(new { error = validation.Message }),
    WhatsAppApiException api => Results.Problem(
        detail: api.Message,
        statusCode: (int)api.StatusCode,
        extensions: new Dictionary<string, object?>
        {
            ["errorCode"] = api.ErrorCode,
            ["errorSubcode"] = api.ErrorSubcode,
            ["errorType"] = api.ErrorType,
            ["metaTraceId"] = api.MetaTraceId,
            ["isTransient"] = api.IsTransient,
            ["retryAfterSeconds"] = api.RetryAfter?.TotalSeconds,
        }),
    _ => throw new UnreachableException(),
};
