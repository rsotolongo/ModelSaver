# ProQualEx.ModelSaver.Core

[![NuGet](https://img.shields.io/nuget/v/ProQualEx.ModelSaver.Core.svg)](https://www.nuget.org/packages/ProQualEx.ModelSaver.Core)

Framework-agnostic core for **ProQualEx.ModelSaver**: attribute-driven, automatic
hidden-field persistence for ASP.NET forms.

Most ASP.NET forms only render the fields the user edits. Everything else on the
model — an `Id`, a `CreatedAtUtc`, a computed `Total` — has to be re-fetched from
the database on postback, or hand-added as a `<input type="hidden">` and kept in
sync by hand as the model changes. ModelSaver removes that boilerplate: mark a
model `[PersistOnPostback]`, and any eligible property that the view doesn't
already render is appended as a hidden field automatically, then comes back
intact on the next request.

This package has no dependency on ASP.NET Core or ASP.NET MVC 5 — it only
implements the attribute, the diffing logic, and the HTML rendering. To use it in
a web app, install one of the framework adapters instead:

| Framework | Package |
|---|---|
| ASP.NET Core (Tag Helpers) | [`ProQualEx.ModelSaver.AspNetCore`](https://www.nuget.org/packages/ProQualEx.ModelSaver.AspNetCore) |
| ASP.NET MVC 5 (`Html.BeginForm`) | [`ProQualEx.ModelSaver.Mvc5`](https://www.nuget.org/packages/ProQualEx.ModelSaver.Mvc5) |

Install this package directly only if you are writing a new adapter for another
rendering stack, or consuming the diffing API outside a web request.

## Install

```bash
dotnet add package ProQualEx.ModelSaver.Core
```

Targets `netstandard2.0`, so it runs on both .NET Framework (via an adapter such
as `ProQualEx.ModelSaver.Mvc5`) and modern .NET.

## How a model becomes eligible

Apply `[PersistOnPostback]` to a class to persist every public, readable
property by default, then opt individual properties out with
`[PersistOnPostback(false)]`:

```csharp
[PersistOnPostback]
public sealed class OrderEditModel
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty; // rendered as a visible input
    public DateTime CreatedAtUtc { get; set; }
    public decimal Total { get; set; }

    [PersistOnPostback(false)]
    public string DisplayOnlySummary { get; set; } = string.Empty; // never persisted
}
```

You can also apply the attribute per property instead of at the class level —
a property-level attribute always wins over the class-level one.

A property is **eligible** to persist when:

- it is `public`, has a getter, and is not indexed, **and**
- it resolves to `[PersistOnPostback(true)]` (explicitly, or by inheriting the
  class-level attribute), **and**
- its type is a scalar value — a primitive, `enum`, `string`, `decimal`,
  `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, or a `Nullable<T>` of any of
  those.

A property whose type is **not** scalar (a nested object such as
`ShippingAddress`) is walked recursively instead of persisted directly: its own
eligible properties are added using a dotted path, e.g. `ShippingAddress.City`,
`ShippingAddress.PostalCode`. This lets a single `[PersistOnPostback]` on the
root model cover an entire object graph without annotating every nested type.

## The diffing API

Framework adapters are thin wrappers around two static classes:

```csharp
// 1. Compare the model's eligible paths against the form HTML already rendered.
IReadOnlyList<PersistField> missing = HiddenFieldDiffer.GetMissingFields(model, renderedFormHtml);

// 2. Render the missing ones as encoded <input type="hidden"> elements.
string hiddenInputsHtml = HiddenFieldRenderer.Render(missing);
```

`GetMissingFields` only adds a field for a path the form doesn't already
contain an input for (matched by name) — so a property you render yourself as a
visible or hidden input is never duplicated. `HiddenFieldRenderer.Render`
HTML-encodes both the field's `name` and `value`, and derives the element's
`id` by replacing `.` with `_` (e.g. `ShippingAddress.City` →
`id="ShippingAddress_City"`).

## Links

- Repository: <https://github.com/rsotolongo/ModelSaver>
- License: MIT
