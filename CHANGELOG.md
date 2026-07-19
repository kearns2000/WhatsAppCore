# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0-alpha.1] - 2026-07-16

### Added

- **WhatsApp.Core** - Core SDK for the Meta WhatsApp Cloud API.
  - `IWhatsAppClient` with strongly typed message sending, read receipts, and media operations.
  - Convenience extension methods (`SendTextAsync`, `SendTemplateAsync`, `SendImageAsync`, and others).
  - `SendRawMessageAsync` escape hatch for forward-compatible message shapes.
  - Named account support via `IWhatsAppClientFactory`.
  - Configuration binding, startup validation, and secret redaction.
  - `WhatsAppApiException` and `WhatsAppValidationException` with structured error metadata.
  - Opt-in safe retries for idempotent media operations only.
  - `ActivitySource` and `Meter` diagnostics.

- **WhatsApp.Core.AspNetCore** - ASP.NET Core webhook integration.
  - `MapWhatsAppWebhook` for GET verification and POST delivery.
  - HMAC-SHA256 signature validation on raw request bodies.
  - Typed webhook event parsing and handler dispatch.
  - `IWhatsAppWebhookDeduplicator` abstraction with in-memory default (`MemoryWhatsAppWebhookDeduplicator`).
  - Sequential and bounded-parallel dispatch modes.
  - Configurable maximum request body size.

- **WhatsApp.Core.Testing** - Test utilities.
  - `FakeWhatsAppClient` with recorded requests and queued responses.
  - `WhatsAppWebhookBuilder` for constructing test payloads.
  - `WhatsAppWebhookTestSignatures` for generating valid test signatures.

- **Sample application** - `WhatsApp.Core.Sample.Api` demonstrating outbound messaging and webhook handlers.

- **Documentation** - Architecture guide, configuration reference, and topic-specific guides in `docs/`.

- **CI/CD** - GitHub Actions `ci.yml` for build, test, pack, and NuGet Trusted Publishing on version tags, plus Dependabot configuration.

[0.1.0-alpha.1]: https://github.com/kearns2000/WhatsAppCore/releases/tag/v0.1.0-alpha.1
