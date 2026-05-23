# Security policy

## Supported versions

Only the latest released minor version of the `Civitas.Id.Sweden*` package family receives security fixes. The packages release in lockstep at a single shared version (see `RELEASING.md`).

| Version | Supported          |
|---------|--------------------|
| Latest  | :white_check_mark: |
| Older   | :x:                |

When a security fix ships, the patched version is published to nuget.org and the affected prior versions are unlisted (via the nuget.org UI) and deprecated with a pointer to the replacement.

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues, discussions, or pull requests.**

Use one of the following private channels:

1. **GitHub Security Advisory** (preferred) — open a private advisory at
   <https://github.com/crippledgeek/civitas-id-net/security/advisories/new>.
2. **Email** — `mattias.carlsson01@gmail.com` with the subject prefix `[civitas-id-net security]`.

Please include:

- A description of the vulnerability and the affected package(s) / version(s).
- Steps to reproduce, ideally with a minimal repro project.
- Any known mitigations or workarounds.
- Whether you intend to publish the finding, and on what timeline.

## What to expect

- **Acknowledgement**: within 5 business days.
- **Initial assessment**: within 14 calendar days, including a severity rating (CVSS 3.1).
- **Fix timeline**:
  - **Critical / High**: patched and published within 30 days of initial assessment, or as soon as a fix is verified.
  - **Medium / Low**: scheduled into the next regular release.
- **Public disclosure**: coordinated with the reporter. The default policy is to disclose after a fix is released, with credit to the reporter unless they request anonymity.

This is a solo open-source project. There is no bug bounty programme. Acknowledgement in release notes and the security advisory is the standard recognition.

## Scope

In scope:

- All packages under the `Civitas.Id.Sweden*` family published from this repository.
- The library code itself, the test suite, and the build / release tooling.

Out of scope:

- Third-party dependencies (`Dapper`, `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.*`, `System.Text.Json`, etc.) — please report those upstream. We will track upstream advisories and bump dependencies as fixes ship.
- Vulnerabilities in consumer applications that use the library — the library's responsibility ends at its API contract.

## Cryptographic posture

The library performs no cryptographic operations. The Swedish Luhn-10 checksum used to validate ID numbers is a non-cryptographic data-integrity check (per Skatteverket's published specification) and is not a security boundary.

## Disclosure history

_(Empty — no advisories filed as of the latest release.)_
