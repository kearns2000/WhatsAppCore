using Microsoft.Extensions.Logging;

namespace WhatsApp.Core.Diagnostics;

/// <summary>
/// Source-generated, structured log messages emitted by this library. Deliberately excludes
/// message content, phone numbers, media URLs, and tokens; only the account name, operation
/// name, message type, and status/error codes are logged.
/// </summary>
internal static partial class WhatsAppLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "[{AccountName}] Starting {Operation}.")]
    public static partial void OperationStarting(ILogger logger, string accountName, string operation);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "[{AccountName}] Completed {Operation} in {ElapsedMilliseconds}ms.")]
    public static partial void OperationSucceeded(ILogger logger, string accountName, string operation, double elapsedMilliseconds);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "[{AccountName}] {Operation} failed with status {StatusCode} (error code {ErrorCode}).")]
    public static partial void ApiOperationFailed(ILogger logger, Exception exception, string accountName, string operation, int statusCode, int? errorCode);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "[{AccountName}] {Operation} failed unexpectedly.")]
    public static partial void OperationFailed(ILogger logger, Exception exception, string accountName, string operation);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "[{AccountName}] {Operation} retrying (attempt {Attempt}) after {DelayMilliseconds}ms.")]
    public static partial void RetryingAfterTransientFailure(ILogger logger, string accountName, string operation, int attempt, double delayMilliseconds);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "[{AccountName}] {Operation} canceled by caller.")]
    public static partial void OperationCanceled(ILogger logger, string accountName, string operation);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "[{AccountName}] AllowInsecureHttp is set to true; Graph API requests may use a non-HTTPS BaseAddress. This must never be enabled in production.")]
    public static partial void InsecureHttpAllowed(ILogger logger, string accountName);
}
