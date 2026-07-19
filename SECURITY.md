# Security Policy

## Supported versions

| Version | Supported |
|---------|-----------|
| 0.1.x   | Yes       |

Security fixes are applied to the latest release in the supported series.

## Reporting a vulnerability

**Do not open a public GitHub issue for security vulnerabilities.**

Instead, report them privately by emailing the maintainer listed in the repository profile, or by using GitHub's [private vulnerability reporting](https://github.com/kearns2000/WhatsAppCore/security/advisories/new) if enabled.

Include:

- A description of the vulnerability and its potential impact
- Steps to reproduce
- Affected versions
- Any suggested fix or mitigation

You can expect an initial response within **72 hours**. We will work with you to understand and address the issue before any public disclosure.

## Security practices in this library

WhatsApp.Core is designed with the following defaults:

- Access tokens, app secrets, and verify tokens are redacted from logs and `ToString()` output.
- Webhook signature validation uses HMAC-SHA256 with constant-time comparison.
- Raw message sends cannot override the configured Graph API endpoint.
- Message bodies and phone numbers are not included in default diagnostic tags or logs.
- HTTPS is required for the Graph API base address unless explicitly overridden for local testing.

## Consumer responsibilities

When integrating WhatsApp.Core into your application:

- Store credentials in a secret manager, not in source control.
- Enable webhook signature validation in production.
- Serve webhook endpoints over HTTPS.
- Do not log inbound message content or outbound template parameters containing personal data.
- Implement durable deduplication for webhook handlers that perform side effects.
- Pin an explicit Graph API version and monitor Meta's deprecation notices.
- Keep dependencies up to date.

## Scope

This policy covers the WhatsApp.Core NuGet packages and the sample application in this repository. It does not cover Meta's Cloud API infrastructure, your hosting environment, or third-party services you integrate with.
