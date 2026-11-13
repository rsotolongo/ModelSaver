# ProQualEx.ModelSaver

[![NuGet (Core)](https://img.shields.io/nuget/v/ProQualEx.ModelSaver.Core.svg?label=Core)](https://www.nuget.org/packages/ProQualEx.ModelSaver.Core)
[![NuGet (AspNetCore)](https://img.shields.io/nuget/v/ProQualEx.ModelSaver.AspNetCore.svg?label=AspNetCore)](https://www.nuget.org/packages/ProQualEx.ModelSaver.AspNetCore)
[![NuGet (Mvc5)](https://img.shields.io/nuget/v/ProQualEx.ModelSaver.Mvc5.svg?label=Mvc5)](https://www.nuget.org/packages/ProQualEx.ModelSaver.Mvc5)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Automatic hidden-field persistence for ASP.NET forms. Mark a model
`[PersistOnPostback]`, render only the fields the user actually edits, and
every other eligible property — an `Id`, a `CreatedAtUtc`, a computed
`Total` — is appended as a hidden `<input>` and comes back intact on the
next postback. No more hand-added hidden fields that silently go stale when
the model changes.

```csharp
[PersistOnPostback]
public sealed class OrderEditModel
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty; // the only visible input
    public DateTime CreatedAtUtc { get; set; }
    public decimal Total { get; set; }

    [PersistOnPostback(false)]
    public string DisplayOnlySummary { get; set; } = string.Empty; // never persisted
}
```

```cshtml
<!-- ASP.NET Core -->
<form asp-action="Save" asp-persist-model="Model">
    <input asp-for="CustomerName" />
    <button type="submit">Save</button>
</form>
```

```cshtml
@* ASP.NET MVC 5 *@
@using (Html.BeginPersistedForm(Model, "Save", "Order"))
{
    @Html.TextBoxFor(m => m.CustomerName)
    <button type="submit">Save</button>
}
```

`Id`, `CustomerId`, `CreatedAtUtc`, and `Total` are appended as hidden
inputs automatically; `DisplayOnlySummary` is skipped because it opts out.
Nested objects work too — `ShippingAddress.City` persists the same way, via
a dotted path matching ASP.NET's own model-binding convention.

## Packages

This repo ships three packages. Install the one matching your framework —
each depends on `Core` transitively, so you never install it directly
unless you're writing a new adapter.

| Package | Targets | Use it for |
|---|---|---|
| [`ProQualEx.ModelSaver.Core`](src/ProQualEx.ModelSaver.Core) | `netstandard2.0` | The attribute, diff algorithm, and HTML renderer — framework-agnostic, no ASP.NET dependency |
| [`ProQualEx.ModelSaver.AspNetCore`](src/ProQualEx.ModelSaver.AspNetCore) | `net10.0` | ASP.NET Core MVC / Razor Pages, via a `<form asp-persist-model="...">` Tag Helper |
| [`ProQualEx.ModelSaver.Mvc5`](src/ProQualEx.ModelSaver.Mvc5) | `net48` | Classic ASP.NET MVC 5 (`System.Web.Mvc`), via an `Html.BeginPersistedForm` helper |

```bash
dotnet add package ProQualEx.ModelSaver.AspNetCore   # or ProQualEx.ModelSaver.Mvc5
```

See each package's own README (linked above) for full usage, or the
runnable samples below for an end-to-end app.

## Samples

| Sample | Shows |
|---|---|
| [`samples/ProQualEx.ModelSaver.AspNetCore.Sample`](samples/ProQualEx.ModelSaver.AspNetCore.Sample) | The ASP.NET Core adapter end-to-end — `dotnet run --project samples/ProQualEx.ModelSaver.AspNetCore.Sample` |
| [`samples/ProQualEx.ModelSaver.Mvc5.Sample`](samples/ProQualEx.ModelSaver.Mvc5.Sample) | The same scenario on classic MVC 5, for side-by-side comparison (Windows + IIS Express) |

## Building and testing

```bash
dotnet build ProQualEx.ModelSaver.slnx -c Release
dotnet test ProQualEx.ModelSaver.slnx -c Release
dotnet format ProQualEx.ModelSaver.slnx --verify-no-changes
```

The solution targets `netstandard2.0`, `net10.0`, and `net48` side by side;
all three build on Linux, macOS, or Windows. Running the `net48` test
project needs a real .NET Framework runtime or `mono` — on a machine
without either, the two `net10.0` test projects still build and pass, and
the `net48` one aborts with a `mono` host error. CI runs on
`windows-latest` to cover all three.

## Releasing

Packages are published to nuget.org by
[`.github/workflows/publish-nuget.yml`](.github/workflows/publish-nuget.yml)
via [NuGet.org Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
(OIDC — no long-lived API key). Pushing a tag matching `v*.*.*` builds,
tests, packs, and publishes all three packages together at that version.

## License

[MIT](LICENSE) © Ricardo Sotolongo
