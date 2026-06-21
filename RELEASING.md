# Releasing

How to cut a release of the seven `Civitas.Id.Sweden*` NuGet packages. Read end-to-end before your first release — the OIDC-trusted-publishing setup has version-specific prerequisites, and a few traps in the NuGet ecosystem differ materially from npm (in particular: provenance attestations do NOT yet work for NuGet consumers — see below).

## Package set

All seven packages release in lockstep at the same version. The version is set once in `Directory.Build.targets` (`<Version>` property) and inherited by every csproj with `IsPackable=true`.

| Package | csproj | Role |
|---|---|---|
| `Civitas.Id.Sweden` | `Civitas.Id.Sweden/Civitas.Id.Sweden.csproj` | Core library — depended on by all five companions. **Push first.** |
| `Civitas.Id.Sweden.Json` | `Civitas.Id.Sweden.Json/Civitas.Id.Sweden.Json.csproj` | `System.Text.Json` converters. |
| `Civitas.Id.Sweden.DataAnnotations` | `Civitas.Id.Sweden.DataAnnotations/Civitas.Id.Sweden.DataAnnotations.csproj` | `ValidationAttribute` family. |
| `Civitas.Id.Sweden.AspNetCore` | `Civitas.Id.Sweden.AspNetCore/Civitas.Id.Sweden.AspNetCore.csproj` | ASP.NET Core 10 integration. |
| `Civitas.Id.Sweden.Fakers` | `Civitas.Id.Sweden.Fakers/Civitas.Id.Sweden.Fakers.csproj` | Algorithmic fakers. |
| `Civitas.Id.Sweden.EntityFrameworkCore` | `Civitas.Id.Sweden.EntityFrameworkCore/Civitas.Id.Sweden.EntityFrameworkCore.csproj` | EF Core 10 `ValueConverter`s. |
| `Civitas.Id.Sweden.Dapper` | `Civitas.Id.Sweden.Dapper/Civitas.Id.Sweden.Dapper.csproj` | Dapper 2.x `SqlMapper.TypeHandler<T>` implementations. |

## Hard prerequisites

- **NuGet trusted publishing must be configured on nuget.org.** As of 2025-09-22, nuget.org supports GitHub-Actions OIDC trusted publishing in GA ([Microsoft Learn](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing), [.NET blog announcement](https://devblogs.microsoft.com/dotnet/enhanced-security-is-here-with-the-new-trust-publishing-on-nuget-org/)). For each of the seven package IDs above, register a trusted publisher policy at `https://www.nuget.org/account/trustedpublishing` with:
  - **Owner**: `crippledgeek`
  - **Repository**: `civitas-id-net`
  - **Workflow filename**: `publish.yml`
  - **Environment** (optional but recommended): `release`
- **GitHub Actions runner must use .NET 10 SDK** (`actions/setup-dotnet@v4` with `dotnet-version: '10.x'`). The `NuGet/login@v1` action requires a recent SDK to exchange OIDC tokens.
- **Workflow permissions must include `id-token: write`** and `contents: read`. OIDC token mint requires the former; checkout requires the latter.
- **No `NUGET_API_KEY` secret is needed** with OIDC. If one exists, delete it — `NuGet/login@v1` mints a short-lived (1-hour, single-use) key per workflow run.
- **Gradual rollout caveat**: if the "Trusted Publishing" tab is not visible on the nuget.org profile, the feature has not yet rolled out to the account. Fall back to a scoped API key for the affected packages; retry OIDC setup after the rollout reaches the account.

## Branch flow (gitflow)

Per `~/.claude/rules/gitflow-branch-policy.md`:

```
feature/* ──┐
bugfix/*  ──┤
chore/*   ──┤        ┌──► PR to develop
spike/*   ──┘
                     ↓
                  develop ──► release/vX.Y.Z ──► PR to master ──► tag vX.Y.Z
                                                      │
                                                      └──► back-merge master → develop
```

- Feature / bugfix / chore / spike branches target `develop`.
- When `develop` is feature-complete for a release, cut `release/vX.Y.Z` from `develop`.
- `release/vX.Y.Z` exists for last-minute version-bump and CHANGELOG edits only — no new features.
- PR `release/vX.Y.Z` → `master`. Merge creates the release commit on `master`.
- Tag `vX.Y.Z` annotated on the merge commit. Push the tag.
- **Back-merge `master` → `develop` is mandatory** to propagate release-branch fixes and keep history aligned.

## Version bumping

All seven packages ship at the same version. To bump:

1. Edit `Directory.Build.targets` line `<Version>X.Y.Z</Version>`.
2. Bump per SemVer:
   - **PATCH** (`1.0.0` → `1.0.1`): backward-compatible bug fix in any of the seven packages.
   - **MINOR** (`1.0.0` → `1.1.0`): backward-compatible feature addition in any package.
   - **MAJOR** (`1.0.0` → `2.0.0`): any removal / rename / behavior change to the public API of any package.

For v1.0.1 and later, also enable package validation against the prior version:

```xml
<EnablePackageValidation>true</EnablePackageValidation>
<PackageValidationBaselineVersion>1.0.0</PackageValidationBaselineVersion>
```

The Baseline validator downloads `1.0.0` from nuget.org during `dotnet pack` and surfaces any binary-breaking change as a build error. Bump `PackageValidationBaselineVersion` to whatever the most-recently-shipped version is.

## Pre-release checklist

Before opening the release PR to `master`:

- [ ] `Directory.Build.targets` `<Version>` bumped.
- [ ] `CHANGELOG.md` has an entry for the new version, dated `YYYY-MM-DD`, with sections matching prior releases (`### Added` / `### Changed` / `### Fixed` / `### Removed`).
- [ ] For major versions: each affected package's `README.md` carries a "vX.0.0 (breaking)" callout near the top listing removed / changed API.
- [ ] **Any docs referenced from the GitHub release page** (e.g. `RELEASING.md`, `CHANGELOG.md`, ADRs under `adr/`) **are present on `master`** at the cut point. Release-notes links typically point at `https://github.com/.../blob/master/<file>`; if the file lives only on `develop`, the link 404s. Verify with `git ls-tree origin/master --name-only <file>`.
- [ ] Local gate clean:
  ```bash
  dotnet format --verify-no-changes
  dotnet build -c Release
  dotnet test
  jb inspectcode CivitasId.slnx --severity=HINT --no-build  # zero issues
  ```
- [ ] CI green on `develop` (`ci.yml` runs `dotnet format` + build + full test on every PR/push).
- [ ] Package validation gate (v1.0.1+): `dotnet pack -c Release` against the bumped version succeeds with no `PKV*` errors. If a binary-breaking change is intentional, document it in the CHANGELOG under `### BREAKING CHANGES` and bump the MAJOR version instead.

## Cutting the release

```bash
git checkout develop
git pull origin develop

git checkout -b release/v1.0.1                # adjust version
# edit Directory.Build.targets <Version>; update CHANGELOG.md; commit
git push -u origin release/v1.0.1

gh pr create --base master --head release/v1.0.1 \
  --title "Release v1.0.1" \
  --body "Release v1.0.1 — <summary>. See CHANGELOG.md for details."
```

Wait for any required reviews / CI checks, then merge:

```bash
gh pr merge <PR#> --merge --delete-branch
```

## Tagging

After the release PR merges to `master`:

```bash
git checkout master
git pull origin master

git tag -a v1.0.1 -m "v1.0.1 — <one-line summary>

<short paragraph or bullet list of headline changes>

See CHANGELOG.md for full details."

git push origin v1.0.1
```

Tags MUST be **annotated** (`-a`), not lightweight. Use the SemVer `vMAJOR.MINOR.PATCH` form. The tag is what the publish workflow checks out and what determines the published package version (because `<Version>` was bumped on the release branch before the merge).

## GitHub Release

```bash
gh release create v1.0.1 --title "v1.0.1" --notes-file <notes-file>
```

Release-note format mirrors prior releases — one section per affected package, each item linking its tracking issue, closing with a link to `CHANGELOG.md`.

**Publishing the GitHub release fires the `Publish to NuGet` workflow.**

## What the publish workflow does

`release: published` → workflow checks out the tagged commit → `actions/setup-dotnet@v4` → `dotnet pack -c Release` → `NuGet/login@v1` exchanges the GitHub OIDC token for a 1-hour scoped nuget.org API key → `dotnet nuget push *.nupkg` for each of the seven packages, with `--skip-duplicate` (safe to re-run) → explicit second loop for `*.snupkg` (symbol packages).

Successful publish emits these markers in the log:

```
Pushing Civitas.Id.Sweden.1.0.1.nupkg to 'https://www.nuget.org/api/v2/package'...
  PUT https://www.nuget.org/api/v2/package/
  Created https://www.nuget.org/api/v2/package/ 200ms
Your package was pushed.
```

A successful `*.snupkg` push reports the same shape. The repository-signing certificate (Microsoft NuGet repo signing, SHA-256 `1F4B311D9ACC115C8DC8018B5A49E00FCE6DA8E2855F9F014CA6F34570BC482D` as of the April 2024 rotation) is applied by nuget.org automatically — no action required from the workflow.

### The publish workflow (live since v1.0.0)

The workflow is live at `.github/workflows/publish.yml` (added for the v1.0.0 release) — that committed file is authoritative. It hardens the reference block below in three ways: actions are pinned to commit SHAs (not floating `@v4`/`@v1`), the SDK is resolved via `global-json-file: global.json` (not `dotnet-version: '10.x'`), and the minted key is passed through a step `env:` var rather than inline `${{ }}`. The block below is kept as the annotated reference; the "Manual publish (pre-CI)" section remains a fallback for when OIDC/trusted publishing is unavailable.

```yaml
name: Publish to NuGet

on:
  release:
    types: [published]
  workflow_dispatch:
    inputs:
      ref:
        description: "Tag or branch to publish from (e.g. v1.0.1)"
        required: true
        default: "master"

jobs:
  publish:
    runs-on: ubuntu-latest
    environment: release          # optional; matches the nuget.org trusted-publisher policy

    permissions:
      contents: read              # checkout
      id-token: write             # mint OIDC token for NuGet/login@v1

    steps:
      - uses: actions/checkout@v4
        with:
          ref: ${{ github.event.inputs.ref || github.ref }}

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'

      # Build + test before pack so a broken build can't ship.
      - run: dotnet restore CivitasId.slnx
      - run: dotnet build CivitasId.slnx -c Release --no-restore
      - run: dotnet test CivitasId.slnx -c Release --no-build

      - run: dotnet pack CivitasId.slnx -c Release --no-build --output ./artifacts
        env:
          CI: true                # triggers ContinuousIntegrationBuild → deterministic pack

      # Exchange the GitHub OIDC token for a short-lived nuget.org API key.
      # The trusted-publisher policy on nuget.org gates which repo+workflow+ref combinations
      # can mint a key. No NUGET_API_KEY secret is configured.
      - uses: NuGet/login@v1
        id: login
        with:
          user: ${{ secrets.NUGET_USER }}   # nuget.org profile name (NOT email)

      - name: Push .nupkg files
        run: |
          for pkg in ./artifacts/*.nupkg; do
            dotnet nuget push "$pkg" \
              --api-key "${{ steps.login.outputs.NUGET_API_KEY }}" \
              --source https://api.nuget.org/v3/index.json \
              --skip-duplicate
          done

      - name: Push .snupkg files
        run: |
          for sym in ./artifacts/*.snupkg; do
            dotnet nuget push "$sym" \
              --api-key "${{ steps.login.outputs.NUGET_API_KEY }}" \
              --source https://api.nuget.org/v3/index.json \
              --skip-duplicate
          done
```

`--skip-duplicate` makes the push loop idempotent — re-running the workflow after a partial failure pushes only the missing packages.

## Manual publish (fallback)

The CI workflow above is the primary path. Use this manual procedure only as a fallback — e.g. if trusted publishing/OIDC is unavailable for the account. Run the equivalent locally from a freshly-checked-out tag:

```bash
git checkout v1.0.1
CI=true dotnet pack CivitasId.slnx -c Release --output ./artifacts

# Generate a scoped, time-limited API key at https://www.nuget.org/account/apikeys
# Scope: "Push new packages and package versions" for the seven Civitas.Id.Sweden* IDs.
# Expiry: 1 day is plenty for a single release.
APIKEY=<paste-key-here>

# Push .nupkg (auto-pushes adjacent .snupkg via the CLI's auto-symbols behaviour,
# but loop explicitly for safety — see Andrew Lock's writeup).
for pkg in ./artifacts/*.nupkg; do
  dotnet nuget push "$pkg" \
    --api-key "$APIKEY" \
    --source https://api.nuget.org/v3/index.json \
    --skip-duplicate
done

# Explicit symbol push.
for sym in ./artifacts/*.snupkg; do
  dotnet nuget push "$sym" \
    --api-key "$APIKEY" \
    --source https://api.nuget.org/v3/index.json \
    --skip-duplicate
done

# Revoke the API key immediately afterward at the same URL.
```

## Back-merge to develop

After the master merge and tagging:

```bash
git checkout develop
git pull origin develop
git merge --no-ff origin/master -m "Merge tag 'v1.0.1' into develop"
git push origin develop
```

This propagates any release-branch fixups (rare — most of the time the release branch is identical to develop at the cut point) and keeps the gitflow back-merge audit trail intact. Verify with:

```bash
git log origin/develop..origin/master --oneline   # MUST be empty
```

If output is non-empty, the back-merge is incomplete — repeat the merge step.

## Post-publish verification

The package detail pages on nuget.org are live within seconds; the search index typically updates within 15 minutes, with anomalous delays up to a few hours on heavy-load days (see [status.nuget.org](https://status.nuget.org)).

```bash
# All seven should report the new version.
for pkg in Civitas.Id.Sweden Civitas.Id.Sweden.Json Civitas.Id.Sweden.DataAnnotations \
           Civitas.Id.Sweden.AspNetCore Civitas.Id.Sweden.Fakers Civitas.Id.Sweden.EntityFrameworkCore \
           Civitas.Id.Sweden.Dapper; do
  printf "%s: " "$pkg"
  curl -s "https://api.nuget.org/v3-flatcontainer/${pkg,,}/index.json" | jq -r '.versions[-1]'
done
```

Check the package detail pages render the README correctly:

- https://www.nuget.org/packages/Civitas.Id.Sweden
- https://www.nuget.org/packages/Civitas.Id.Sweden.Json
- https://www.nuget.org/packages/Civitas.Id.Sweden.DataAnnotations
- https://www.nuget.org/packages/Civitas.Id.Sweden.AspNetCore
- https://www.nuget.org/packages/Civitas.Id.Sweden.Fakers
- https://www.nuget.org/packages/Civitas.Id.Sweden.EntityFrameworkCore
- https://www.nuget.org/packages/Civitas.Id.Sweden.Dapper

Verify Source Link by installing one package into a throwaway console app, setting a breakpoint inside a library call, and stepping into the source in Rider / Visual Studio. The debugger should fetch source from GitHub at the tagged commit. If symbols don't load: confirm the `.snupkg` push succeeded and that `symbols.nuget.org` reports the package indexed (15-min SLA).

## Recovering from a failed publish

If the workflow fails or a local push errors mid-flight:

1. **Diagnose** the failure. Common causes:
   - Trusted publisher policy not configured for the affected package → add the policy on nuget.org, re-run the workflow.
   - OIDC permissions block missing `id-token: write` → fix the workflow YAML, re-run.
   - `NuGet/login@v1` action not pinned to a working version → pin to a known commit SHA.
   - Network / nuget.org transient errors → re-run; `--skip-duplicate` handles partial state.

2. **Re-run** without recreating the GitHub release. The workflow has a `workflow_dispatch` trigger with a `ref` input:
   ```bash
   gh workflow run "Publish to NuGet" --ref master --field ref=v1.0.1
   ```
   This re-publishes against the existing tag.

3. **Broken package shipped (e.g. critical bug discovered post-publish).** nuget.org does NOT support self-service permanent deletion. The remediation flow is:
   1. **Unlist** the broken version via the nuget.org UI (hides it from search; `dotnet add package` without a version specifier won't pull it; existing dependents continue to resolve it from their lockfiles).
   2. **Publish a patched version** (PATCH bump) with the fix.
   3. **Deprecate** the broken version via the nuget.org "Deprecate" UI with a message pointing to the replacement. Consumers see a warning in Visual Studio / Rider when they pin to the deprecated version.

   Permanent deletion is restricted to ToS / copyright / malware violations and requires a support request to the NuGet team.

## Hotfixes

For production defects discovered after a release:

```
master ──► hotfix/<topic> ──► PR to master ──► tag vX.Y.Z+1
                                  │
                                  └──► back-merge master → develop
```

- Hotfix branches come from `master` (not `develop`).
- Same tag + release + back-merge sequence as a normal release.
- If a `release/*` branch is open at the time, merge the hotfix into the release branch instead of `develop` — it reaches `develop` when the release branch back-merges.
- Bump the PATCH digit, not MINOR or MAJOR. A hotfix is an emergency fix to the last-shipped state, not a new minor release.

## Signing — what we use and don't use

- **Repository signing (automatic, free, applied by nuget.org).** Every `.nupkg` and `.snupkg` uploaded to nuget.org is counter-signed with the Microsoft NuGet repository signing certificate at upload time. This guarantees package integrity end-to-end: consumers can verify with `dotnet nuget trust list` and `dotnet nuget verify <package>`. **This happens automatically — no workflow step required.**

- **Author signing (optional, not used).** `dotnet nuget sign` supports author signing with a public-CA-issued X.509 code-signing certificate (~$300+/year). Once any certificate is registered against the nuget.org account, **all future uploads from that account MUST be signed**. The cost vs added consumer-visible benefit is not worth it for a solo OSS developer when repository signing already provides the integrity guarantee. Revisit if the project moves to an organisation account.

## Provenance attestations — currently unsupported on NuGet

Unlike the npm + Sigstore model used by the TypeScript sibling (`@deathbycode/civitas-id-*`), NuGet does NOT yet have a usable Sigstore-style provenance attestation pipeline for consumers. The mechanics exist — GitHub's `actions/attest-build-provenance` can record a SLSA Build Level 2 attestation for a `.nupkg` to the GitHub attestation store and the Sigstore Public Good Instance — but the attestation is functionally useless because **nuget.org modifies every uploaded package at the registry**: a `.signature.p7s` repository-signing file is injected into the `.nupkg` archive, changing its SHA-256 fingerprint. The attestation was created for the pre-upload artifact; the artifact consumers download is different. There is no NuGet client support for verifying provenance attestations at install time.

Tracking the gap:

- [Andrew Lock: Creating provenance attestations for NuGet packages](https://andrewlock.net/creating-provenance-attestations-for-nuget-packages-in-github-actions/) documents the SHA-mismatch problem and current workarounds.
- The fix requires changes to the nuget.org upload pipeline. No committed milestone as of 2026-05.

**Recommendation: skip provenance attestations until nuget.org changes its upload behaviour.** Re-evaluate before a v2.0 release.

## Shipped releases

| Version | Date | Notes |
|---|---|---|
| **v1.0.0** | 2026-06-21 | Initial public release of all seven packages via OIDC trusted publishing. Tag `v1.0.0` on master merge commit `84104cf`; published through `.github/workflows/publish.yml` with the `release` environment manual-approval gate. All seven IDs live on nuget.org; download index resolved ~7 min after the approved push. |

## Past trips

Documenting failures so we don't repeat them. _(No **publish** failures to date. v1.0.0 (2026-06-21) shipped cleanly — though three issues were caught by CI **before** the publish and fixed pre-release: a duplicate-README pack failure (NU5118), the transitive SQLite CVE-2025-6965 (NU1903), and a TUnit/TUnit.FsCheck version drift that made the 51k-test suite silently run zero tests. See git history on the `chore/ci-publish-workflow` and `bugfix/efcore-test-model-race` branches. Update on every observed publish failure.)_

<!--
Template:

- **vX.Y.Z** — <one-line cause>. <Short description of what happened and what the fix was>. **Lesson**: <forward-looking guidance>.
-->

## References

- [Trusted Publishing on nuget.org — Microsoft Learn](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) — official docs, GA announcement, policy setup.
- [.NET blog: Enhanced Security with Trusted Publishing](https://devblogs.microsoft.com/dotnet/enhanced-security-is-here-with-the-new-trust-publishing-on-nuget-org/) — 2025-09-22 GA announcement.
- [Andrew Lock: Easily publishing NuGet packages with Trusted Publishing](https://andrewlock.net/easily-publishing-nuget-packages-from-github-actions-with-trusted-publishing/) — full GitHub Actions workflow walkthrough.
- [Andrew Lock: Creating provenance attestations for NuGet packages](https://andrewlock.net/creating-provenance-attestations-for-nuget-packages-in-github-actions/) — Sigstore + NuGet gap.
- [Symbol packages (.snupkg) — Microsoft Learn](https://learn.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg) — snupkg format, indexing SLA, Portable PDB requirement.
- [Package Validation overview — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview) — `EnablePackageValidation` semantics.
- [Baseline version validator — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/baseline-version-validator) — `PackageValidationBaselineVersion` workflow.
- [NuGet signed-package verification — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/nuget-signed-package-verification) — consumer verification of repository signing.
- [Deleting packages — Microsoft Learn](https://learn.microsoft.com/en-us/nuget/nuget-org/policies/deleting-packages) — unlist vs delete policy.
- [gitflow-branch-policy.md](https://github.com/crippledgeek/civitas-id-net/blob/master/.gitignore) (user rule at `~/.claude/rules/gitflow-branch-policy.md`) — branch naming, allowed bases, back-merge requirements.
- TypeScript sibling: [`crippledgeek/civitas-id/RELEASING.md`](https://github.com/crippledgeek/civitas-id/blob/master/RELEASING.md) — npm OIDC equivalent (different ecosystem, similar shape).
