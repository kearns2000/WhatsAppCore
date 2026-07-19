using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhatsApp.Core.Configuration;

namespace WhatsApp.Core.Tests;

public sealed class WhatsAppOptionsValidatorTests
{
    private readonly WhatsAppOptionsValidator _validator = new();

    [Fact]
    public void ValidOptions_Succeeds()
    {
        var result = _validator.Validate("Default", CreateValidOptions());
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void RejectsLatestGraphApiVersion()
    {
        var options = CreateValidOptions();
        options.GraphApiVersion = "latest";

        var result = _validator.Validate("Default", options);
        Assert.False(result.Succeeded);
        Assert.Contains("latest", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsInsecureHttpWithoutAllowFlag()
    {
        var options = CreateValidOptions();
        options.BaseAddress = new Uri("http://localhost/");

        var result = _validator.Validate("Default", options);
        Assert.False(result.Succeeded);
        Assert.Contains("HTTPS", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void AllowsInsecureHttpWhenExplicitlyEnabled()
    {
        var options = CreateValidOptions();
        options.BaseAddress = new Uri("http://localhost/");
        options.AllowInsecureHttp = true;

        var result = _validator.Validate("Default", options);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void WarnsWhenAllowInsecureHttpIsEnabled()
    {
        var logger = new CollectingLogger<WhatsAppOptionsValidator>();
        var validator = new WhatsAppOptionsValidator(logger);
        var options = CreateValidOptions();
        options.BaseAddress = new Uri("http://localhost/");
        options.AllowInsecureHttp = true;

        var result = validator.Validate("Default", options);

        Assert.True(result.Succeeded);
        Assert.Contains(
            logger.Messages,
            message => message.Contains("AllowInsecureHttp", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsNonPositiveTimeout()
    {
        var options = CreateValidOptions();
        options.Timeout = TimeSpan.Zero;

        var result = _validator.Validate("Default", options);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void PrefixesFailuresWithAccountName()
    {
        var options = CreateValidOptions();
        options.PhoneNumberId = "";

        var result = _validator.Validate("Sales", options);
        Assert.False(result.Succeeded);
        Assert.Contains("[Sales]", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ToString_RedactsSecrets()
    {
        var options = CreateValidOptions();
        options.AccessToken = "super-secret-token";
        options.AppSecret = "app-secret";
        options.VerifyToken = "verify-token";
        options.PhoneNumberId = "1234567890123";

        var text = options.ToString();
        Assert.DoesNotContain("super-secret-token", text, StringComparison.Ordinal);
        Assert.DoesNotContain("app-secret", text, StringComparison.Ordinal);
        Assert.DoesNotContain("verify-token", text, StringComparison.Ordinal);
        Assert.Contains("***", text, StringComparison.Ordinal);
        Assert.Contains("0123", text, StringComparison.Ordinal);
    }

    private static WhatsAppOptions CreateValidOptions() => new()
    {
        PhoneNumberId = "123456789",
        AccessToken = "token",
        GraphApiVersion = "v21.0",
        BaseAddress = new Uri("https://graph.facebook.com/"),
    };

    private sealed class CollectingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
