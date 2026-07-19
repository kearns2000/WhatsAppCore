# Contributing to WhatsApp.Core

Thank you for your interest in contributing. This document explains how to get started.

## Code of conduct

Be respectful and constructive. Focus on the technical merits of proposals and reviews.

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git

### Clone and build

```bash
git clone https://github.com/kearns2000/WhatsAppCore.git
cd WhatsApp.Core
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

### Formatting

The CI pipeline verifies formatting. Run before submitting a pull request:

```bash
dotnet format
```

To check without applying changes:

```bash
dotnet format --verify-no-changes
```

## Project structure

| Path | Purpose |
|------|---------|
| `src/WhatsApp.Core/` | Core NuGet package |
| `src/WhatsApp.Core.AspNetCore/` | ASP.NET Core webhook integration |
| `src/WhatsApp.Core.Testing/` | Test doubles and builders |
| `samples/WhatsApp.Core.Sample.Api/` | Reference sample application |
| `tests/` | Unit and integration tests |
| `docs/` | Topic guides |

## Making changes

1. **Fork** the repository and create a feature branch from `main`.
2. **Write tests** for new behavior. Bug fixes should include a regression test.
3. **Keep the public API small.** Avoid unnecessary abstractions and interfaces.
4. **Document public APIs** with XML comments on every public type and member.
5. **Do not log secrets or PII** - no access tokens, message bodies, or complete phone numbers.
6. **Run the full test suite** before opening a pull request.
7. **Update documentation** when behavior or configuration changes.

## Pull request guidelines

- Use a clear title and description explaining the motivation and approach.
- Keep pull requests focused - one logical change per PR when possible.
- Ensure CI passes (build, format, tests, pack).
- Do not include real credentials, secrets, or personal data in commits.

## Testing

```bash
dotnet test --configuration Release --no-build
```

For webhook integration tests, see `tests/WhatsApp.Core.AspNetCore.Tests/`. For client and serialization tests, see `tests/WhatsApp.Core.Tests/`.

## Versioning and releases

Releases are tagged with SemVer (for example `v0.1.0`). Pre-release suffixes (`alpha`, `beta`, `rc`) are used until the API stabilizes.

Update `CHANGELOG.md` under `[Unreleased]` when preparing a release. Maintainers cut releases by pushing a `v*` tag; the `ci.yml` workflow publishes to NuGet.org via Trusted Publishing and creates a GitHub release.

## Reporting issues

Use GitHub Issues for bugs and feature requests. Include:

- Package version
- .NET version
- Minimal reproduction steps or code sample
- Expected vs. actual behavior

For security vulnerabilities, see [SECURITY.md](SECURITY.md) - do not open public issues for those.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
