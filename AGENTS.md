# AGENTS.md

This file provides guidance to AI coding agents (including Claude Code, OpenAI Codex, OpenCode, and other compatible agents) when working with code in this repository.

## What this is

ProQualEx.ModelSaver removes the boilerplate of manually adding hidden `<input>`
fields for model properties a form doesn't render. Mark a model
`[PersistOnPostback]`; a form-scoped helper diffs the model's eligible
properties against the HTML the form actually rendered, and appends hidden
inputs for whatever's missing, so a postback round-trips the whole model
without hand-written hidden fields.

Three NuGet packages, in dependency order:

- **`ProQualEx.ModelSaver.Core`** (`netstandard2.0`) — the attribute, the diff
  algorithm, and the HTML renderer. No ASP.NET dependency; pure reflection and
  string processing, fully unit-testable without any web host.
- **`ProQualEx.ModelSaver.AspNetCore`** (`net10.0`) — a Tag Helper adapter
  (`<form asp-persist-model="Model">`).
- **`ProQualEx.ModelSaver.Mvc5`** (`net48`, classic `System.Web.Mvc`) — an
  `Html.BeginPersistedForm` adapter mirroring `Html.BeginForm`.

## Commands

```bash
dotnet build ProQualEx.ModelSaver.slnx -c Release      # solution file is .slnx, not .sln; warnings are errors
dotnet test ProQualEx.ModelSaver.slnx -c Release        # see "net48 tests" caveat below
dotnet format ProQualEx.ModelSaver.slnx --verify-no-changes
dotnet run --project samples/ProQualEx.ModelSaver.AspNetCore.Sample
```

Run a single test (xUnit filter works the same whether it's an exact name or a
substring):

```bash
dotnet test tests/ProQualEx.ModelSaver.Core.Tests --filter "FullyQualifiedName~HiddenFieldDifferTests"
dotnet test tests/ProQualEx.ModelSaver.Core.Tests --filter "FullyQualifiedName~GetMissingFieldsNestedPropertyUsesDottedNameForDiff"
```

**net48 tests on Linux**: `ProQualEx.ModelSaver.Mvc5.Tests` targets `net48`.
The whole solution *builds* fine on Linux (the SDK restores
`Microsoft.NETFramework.ReferenceAssemblies` automatically), but *running*
that test project needs either a real .NET Framework runtime (Windows) or
`mono` on PATH — without one, `dotnet test` on the slnx will build and pass
the two `net10.0` test projects, then abort the `net48` one with
`Could not find 'mono' host`. The GitHub Actions release workflow (below)
therefore runs on `windows-latest`, not `ubuntu-latest`.

Packing a package locally with an explicit version (CI does this per-project,
not via the slnx):

```bash
dotnet pack src/ProQualEx.ModelSaver.Core/ProQualEx.ModelSaver.Core.csproj -c Release -o artifacts -p:Version=1.2.3
```

## Architecture: the diff pipeline

The non-obvious part isn't any single file — it's how `Core`'s pieces compose,
and that both adapters are thin wrappers around the exact same three calls.
Reading just one file in `src/ProQualEx.ModelSaver.Core/` or
`Internal/` won't show the whole picture:

1. **`EligiblePathCollector.Collect(model)`** walks the model's public
   properties via reflection, recursively, building dotted paths
   (`Address.City`) that match ASP.NET's own model-binding naming convention.
   - **`PersistenceResolver.ShouldPersist(property)`** decides per property:
     a property-level `[PersistOnPostback]` always wins; otherwise it falls
     back to the *declaring class's* `[PersistOnPostback]`; otherwise it's
     excluded. Nesting does **not** inherit the parent's resolved value — a
     nested type's own properties are resolved against the nested type's own
     attributes, independently.
   - **`ScalarTypes.IsScalar(type)`** decides where recursion stops: a scalar
     (primitive, `enum`, `string`, `decimal`, `DateTime`, `DateTimeOffset`,
     `TimeSpan`, `Guid`, or `Nullable<T>` of any of those) becomes a leaf
     path; anything else is recursed into (or skipped entirely if `null` —
     no hidden fields are emitted for a null nested object, matching the
     default model binder's own behavior for null nested objects).
2. **`RenderedNameScanner.ExtractNames(html)`** finds every `name="..."` /
   `name='...'` already in the form's rendered HTML. This is a **regex scan,
   not an HTML parser** — a deliberate choice, because the input is always
   Razor/Tag-Helper-generated markup, not arbitrary untrusted HTML. It
   excludes `<input type="submit"|"button"|"reset"|"image">` so a
   coincidentally-named button can't suppress a real hidden field.
3. **`HiddenFieldDiffer.GetMissingFields(model, renderedFormHtml)`** is the
   public entry point both adapters call: it's just
   `eligiblePaths − renderedNames`, each missing path formatted to a string
   via **`PersistValueFormatter.Format`** — deliberately
   `Convert.ToString(value, CultureInfo.CurrentCulture)`, matching
   `Html.HiddenFor`/`Html.TextBoxFor`'s own formatting convention rather than
   `InvariantCulture` (this was corrected during design review specifically
   to avoid a silent mismatch with the rest of the form's fields).
4. **`HiddenFieldRenderer.Render(fields)`** emits
   `<input type="hidden" name="..." id="..." value="..." />` per field,
   HTML-encoding `name`/`value` and deriving `id` from `name` by replacing
   `.` with `_` (so `Address.City` → `id="Address_City"`, matching
   `Html.HiddenFor`'s own id convention).

**Where the two adapters diverge** (same three calls above, different
plumbing to get "the form's rendered HTML" and splice the result back in):

- `PersistOnPostbackTagHelper` (`AspNetCore`) runs at `Order = int.MaxValue`
  so it executes after every other Tag Helper inside the form has already
  rendered; it reads `output.GetChildContentAsync()` directly — no buffering
  trick needed, Tag Helpers already give you the rendered child content
  before the tag closes.
- `BeginPersistedForm` (`Mvc5`) has no Tag Helper equivalent to lean on, so
  `PersistedFormScope` swaps `ViewContext.Writer` for an in-memory
  `StringWriter` for the lifetime of the `using` block, lets the real
  `Html.BeginForm` write into it, and on `Dispose()` runs
  `FormHtmlSplicer.Splice` against the fully-buffered `<form>...</form>`
  string, inserting missing fields immediately before the closing `</form>`,
  before restoring the original writer and flushing the result.

v1 is scoped to flat scalars and single nested objects only — no collections
or indexed paths (`Items[0].Id`) and no tamper protection/signing of hidden
values. Both are deliberate v1 exclusions, not oversights — don't add either
without a wider API change, since collections would need indexed paths and
tamper protection would change the hidden-value format for everyone.

## Testing conventions actually used here

- Plain xUnit (`[Fact]`/`Assert.*`) — no FluentAssertions, NSubstitute, or
  AngleSharp/HTML-parsing library anywhere in the solution. There's no I/O to
  substitute: `Core`'s tests exercise pure functions directly, and the
  adapter tests build `TagHelperContext`/`TagHelperOutput` or raw HTML
  strings by hand rather than hosting a real server.
- Test names are PascalCase with no underscores,
  `<Method><Scenario><Expected>` (e.g.
  `GetMissingFieldsNestedPropertyUsesDottedNameForDiff`).
- `tests/ProQualEx.ModelSaver.Core.Tests` has no ASP.NET dependency at all and
  exercises the attribute-resolution matrix (none/class-only/property-only/
  both/override, independent nested-type resolution) plus the name-scanner's
  quoting/self-closing/excluded-type edge cases — read it before assuming a
  resolution or scanning rule beyond what's documented above.

## Release / publishing

`.github/workflows/publish-nuget.yml` builds, tests, format-checks, packs,
and pushes all three packages to nuget.org **together, in lockstep**, all
stamped with one version taken from the triggering git tag (`v1.2.3` → packs
as `1.2.3`), not from whatever's in each `.csproj`'s `<Version>`. Triggers:
pushing a tag matching `v*.*.*`, or a manual `workflow_dispatch` run with an
explicit version input. Publishes via NuGet.org **Trusted Publishing**
(OIDC) rather than a long-lived API key: the job requests a GitHub OIDC
token (`permissions: id-token: write`), exchanges it for a short-lived
NuGet API key via the `NuGet/login` action, and uses that key for the push.
This needs a matching Trusted Publishing policy configured on nuget.org
(repo owner + repo name + workflow file name `publish-nuget.yml`, scoped to
`ProQualEx.ModelSaver.*`) and a `NUGET_USER` repository secret holding the
nuget.org profile name — no `NUGET_API_KEY` secret exists or is needed.

After a successful push, the job re-syncs to `origin/main` (not whatever
detached commit the tag checked out) and commits the released version into
all three `<Version>` tags. That commit's message ends in `[skip ci]` and
won't retrigger this workflow itself (it only triggers on
tags/`workflow_dispatch`), but it is a direct, unreviewed push straight to
`main` — know that before changing this step.

`main` is protected by a repository **ruleset** (not classic branch
protection — see §below), requiring a pull request. The default
`GITHUB_TOKEN` authenticates as the `github-actions` app, which isn't on
the bypass list, so it can't push here. Neither can a PAT — **confirmed
empirically** (not documented): a fine-grained PAT scoped to Contents:
Read/write, and separately a classic PAT with the `repo` scope, both got
rejected with the exact same `GH013` "Changes must be made through a pull
request" error as a plain unprivileged push, despite both belonging to the
repo admin's account. GitHub evidently doesn't recognize either PAT type as
the `Repository admin` role for ruleset bypass on a personal (non-org)
repo — only a real authenticated-user push (SSH/browser session) or a
**Deploy Key** does. This step pushes over SSH using
`secrets.RELEASE_DEPLOY_KEY` (the private half of a write-access deploy
key added to the repo, see below) for exactly that reason. If that secret
is ever missing/invalid, this step — and only this step — fails; the
actual NuGet publish above it has already succeeded. Don't "simplify" this
back to a PAT without re-testing — it was tried twice and failed both
times.

### Branch protection

`main` has a ruleset (Settings → Rules → Rulesets → "Protect main"), not
classic branch protection: requires a PR + 1 approval, blocks force-pushes
and deletions. Bypass list: `Repository admin` role (`actor_id: 5`,
confirmed empirically — GitHub doesn't document the numeric role IDs) and
`DeployKey` (`actor_id: null`, matches any deploy key on the repo — there's
currently exactly one, "Release automation (write)", write-access, used by
the release workflow's bump step via `RELEASE_DEPLOY_KEY`). Outside
contributors can only land changes via a reviewed PR; the repo admin and
that deploy key can still push straight to `main`. Secret scanning + push
protection and Dependabot security updates are also enabled at the repo
level (`Settings → Code security`) — both free for a public repo.

Shared package metadata (`Authors`, `PackageLicenseExpression=MIT`,
`RepositoryUrl`, `PackageProjectUrl`) lives once in the root
`Directory.Build.props`, not per-project — change it there, not in each
`.csproj`. Each package's own `README.md` (next to its `.csproj`) is packed
into the `.nupkg` via `PackageReadmeFile`; update that file, not a separate
doc, when a package's public API changes.

## Repo-wide constraints worth knowing before you edit anything

- The root `.editorconfig` sets every analyzer diagnostic (StyleCop,
  Roslynator, SonarAnalyzer, Meziantou, SecurityCodeScan — all wired in via
  `Directory.Packages.props` as `GlobalPackageReference`s) to **error**, and
  `Directory.Build.props` sets `TreatWarningsAsErrors`. A build with any
  analyzer violation fails outright; there is no warning-only state.
- `Directory.Packages.props` centrally manages all package versions
  (`ManagePackageVersionsCentrally=true`) — add a version there, not as an
  inline `Version` attribute on a `PackageReference`.
- `samples/` and `tests/` each have their own `.editorconfig` relaxing a few
  rules that don't make sense there: both turn off `CA2007`
  (`.ConfigureAwait`), `CA1515` (types must be internal — sample controllers
  and test classes need to be public), and `CS1591` (missing `///` on public
  members — so, unlike the general .NET XML-doc expectation, test and sample
  classes here don't need one). `tests/` additionally turns off `CA1707`
  (no underscores in identifiers), though the existing tests don't use
  underscores anyway (see naming convention above) — that's headroom, not a
  convention change.
