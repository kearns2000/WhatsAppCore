using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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
        using var _ = new EnvironmentVariableScope("ASPNETCORE_ENVIRONMENT", "Development");
        var options = CreateValidOptions();
        options.BaseAddress = new Uri("http://localhost/");
        options.AllowInsecureHttp = true;

        var result = _validator.Validate("Default", options);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void WarnsWhenAllowInsecureHttpIsEnabled()
    {
        using var _ = new EnvironmentVariableScope("ASPNETCORE_ENVIRONMENT", "Development");
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
    public void RejectsInsecureHttpOutsideDevelopmentLikeEnvironments()
    {
        using var _ = new EnvironmentVariableScope("ASPNETCORE_ENVIRONMENT", "Staging");
        var options = CreateValidOptions();
        options.BaseAddress = new Uri("http://localhost/");
        options.AllowInsecureHttp = true;

        var result = _validator.Validate("Default", options);
        Assert.False(result.Succeeded);
        Assert.Contains("Development", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void PrefersIHostEnvironmentOverProcessEnvironmentVariables()
    {
        using var _ = new EnvironmentVariableScope("ASPNETCORE_ENVIRONMENT", "Staging");
        var validator = new WhatsAppOptionsValidator(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<WhatsAppOptionsValidator>.Instance,
            new FixedHostEnvironment(Environments.Development));
        var options = CreateValidOptions();
        options.BaseAddress = new Uri("http://localhost/");
        options.AllowInsecureHttp = true;

        var result = validator.Validate("Default", options);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void RejectsInsecureHttpWhenIHostEnvironmentIsStaging()
    {
        using var _ = new EnvironmentVariableScope("ASPNETCORE_ENVIRONMENT", "Development");
        var validator = new WhatsAppOptionsValidator(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<WhatsAppOptionsValidator>.Instance,
            new FixedHostEnvironment(Environments.Staging));
        var options = CreateValidOptions();
        options.BaseAddress = new Uri("http://localhost/");
        options.AllowInsecureHttp = true;

        var result = validator.Validate("Default", options);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void RejectsBarePublicSuffixInAllowedMediaDownloadHosts()
    {
        var options = CreateValidOptions();
        options.AllowedMediaDownloadHosts.Add(".com");

        var result = _validator.Validate("Default", options);
        Assert.False(result.Succeeded);
        Assert.Contains("multi-label", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
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

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _previous;

        public EnvironmentVariableScope(string name, string value)
        {
            _name = name;
            _previous = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose() => Environment.SetEnvironmentVariable(_name, _previous);
    }

    private sealed class FixedHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "WhatsApp.Core.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
