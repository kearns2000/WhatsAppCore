using System.Diagnostics;
using System.IO.Compression;

namespace WhatsApp.Core.PackageTests;

public sealed class PackageRestoreAndCompileTests
{
    [Fact]
    public async Task LocalPackages_RestoreAndCompileConsumerProject()
    {
        var root = Path.Combine(Path.GetTempPath(), "whatsapp-core-package-tests", Guid.NewGuid().ToString("N"));
        var packageDir = Path.Combine(root, "packages");
        var consumerDir = Path.Combine(root, "consumer");
        Directory.CreateDirectory(packageDir);
        Directory.CreateDirectory(consumerDir);

        try
        {
            var repoRoot = FindRepositoryRoot();
            await RunDotnetAsync(repoRoot, $"pack \"{Path.Combine(repoRoot, "src/WhatsApp.Core/WhatsApp.Core.csproj")}\" -c Release -o \"{packageDir}\" --property:Version=9.9.9-test");
            await RunDotnetAsync(repoRoot, $"pack \"{Path.Combine(repoRoot, "src/WhatsApp.Core.AspNetCore/WhatsApp.Core.AspNetCore.csproj")}\" -c Release -o \"{packageDir}\" --property:Version=9.9.9-test");
            await RunDotnetAsync(repoRoot, $"pack \"{Path.Combine(repoRoot, "src/WhatsApp.Core.Testing/WhatsApp.Core.Testing.csproj")}\" -c Release -o \"{packageDir}\" --property:Version=9.9.9-test");

            AssertPackagedProjects(packageDir);

            await File.WriteAllTextAsync(Path.Combine(consumerDir, "nuget.config"), $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <packageSources>
                    <clear />
                    <add key="local" value="{packageDir}" />
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                  </packageSources>
                </configuration>
                """);

            await File.WriteAllTextAsync(Path.Combine(consumerDir, "Consumer.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk.Web">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="WhatsApp.Core" Version="9.9.9-test" />
                    <PackageReference Include="WhatsApp.Core.AspNetCore" Version="9.9.9-test" />
                    <PackageReference Include="WhatsApp.Core.Testing" Version="9.9.9-test" />
                  </ItemGroup>
                </Project>
                """);

            await File.WriteAllTextAsync(Path.Combine(consumerDir, "Program.cs"), """
                using WhatsApp.Core.Client;
                using WhatsApp.Core.DependencyInjection;
                using WhatsApp.Core.AspNetCore.DependencyInjection;
                using WhatsApp.Core.AspNetCore.Webhooks;
                using WhatsApp.Core.Testing.Fakes;

                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddWhatsAppCore(options =>
                {
                    options.PhoneNumberId = "123";
                    options.AccessToken = "token";
                    options.GraphApiVersion = "v21.0";
                });
                builder.Services.AddWhatsAppWebhooks();
                builder.Services.AddSingleton<IWhatsAppClient>(new FakeWhatsAppClient());

                var app = builder.Build();
                app.MapWhatsAppWebhook("/webhooks/whatsapp");
                app.Run();
                """);

            await RunDotnetAsync(consumerDir, "restore");
            await RunDotnetAsync(consumerDir, "build -c Release --no-restore");
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for temp directories.
            }
        }
    }

    private static void AssertPackagedProjects(string packageDir)
    {
        var packages = Directory.GetFiles(packageDir, "*.nupkg").Select(Path.GetFileName).ToArray();
        Assert.Contains(packages, p => p!.StartsWith("WhatsApp.Core.", StringComparison.Ordinal) && !p.Contains("AspNetCore") && !p.Contains("Testing"));
        Assert.Contains(packages, p => p!.StartsWith("WhatsApp.Core.AspNetCore.", StringComparison.Ordinal));
        Assert.Contains(packages, p => p!.StartsWith("WhatsApp.Core.Testing.", StringComparison.Ordinal));

        foreach (var nupkgPath in Directory.GetFiles(packageDir, "*.nupkg"))
        {
            using var archive = ZipFile.OpenRead(nupkgPath);
            Assert.Contains(archive.Entries, e => e.FullName.StartsWith("lib/", StringComparison.Ordinal));
            Assert.DoesNotContain(archive.Entries, e => e.FullName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "WhatsApp.Core.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private static async Task RunDotnetAsync(string workingDirectory, string arguments)
    {
        var psi = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet.");
        var stdout = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);

        Assert.True(process.ExitCode == 0, $"dotnet {arguments} failed.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
    }
}
