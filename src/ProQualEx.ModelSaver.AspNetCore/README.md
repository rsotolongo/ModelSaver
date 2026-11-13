# ProQualEx.ModelSaver.AspNetCore

[![NuGet](https://img.shields.io/nuget/v/ProQualEx.ModelSaver.AspNetCore.svg)](https://www.nuget.org/packages/ProQualEx.ModelSaver.AspNetCore)

ASP.NET Core Tag Helper adapter for
[`ProQualEx.ModelSaver`](https://github.com/rsotolongo/ModelSaver): add one
attribute to a `<form>` and every eligible model property that isn't already
rendered is appended as a hidden input automatically, then comes back intact
on the next postback.

## Install

```bash
dotnet add package ProQualEx.ModelSaver.AspNetCore
```

Targets `net10.0` and requires `Microsoft.AspNetCore.App` (an ASP.NET Core MVC
or Razor Pages project). No further registration is needed — Razor's default
Tag Helper discovery picks it up once the package is referenced, same as the
built-in `asp-for` helpers.

> If your project doesn't already have a `_ViewImports.cshtml` that adds
> Tag Helpers from your own assembly with `@addTagHelper *, <YourAssembly>`,
> add `@addTagHelper *, ProQualEx.ModelSaver.AspNetCore` to it.

## Usage

1. Mark the model `[PersistOnPostback]` (from `ProQualEx.ModelSaver.Core`,
   pulled in as a transitive dependency):

   ```csharp
   using ProQualEx.ModelSaver;

   [PersistOnPostback]
   public sealed class OrderEditModel
   {
       public int Id { get; set; }
       public int CustomerId { get; set; }
       public string CustomerName { get; set; } = string.Empty;
       public DateTime CreatedAtUtc { get; set; }
       public decimal Total { get; set; }

       [PersistOnPostback(false)]
       public string DisplayOnlySummary { get; set; } = string.Empty;
   }
   ```

2. Add `asp-persist-model` to the `<form>` and only render the fields the user
   actually edits:

   ```cshtml
   @model OrderEditModel

   <form asp-action="Save" asp-persist-model="Model">
       <input asp-for="CustomerName" />
       <button type="submit">Save</button>
   </form>
   ```

At render time, the tag helper scans the form's already-rendered inner HTML,
diffs it against `Model`'s eligible properties (see
[`ProQualEx.ModelSaver.Core`](https://www.nuget.org/packages/ProQualEx.ModelSaver.Core)
for exactly which properties qualify, including nested objects via dotted
paths like `ShippingAddress.City`), and appends a hidden `<input>` for every
one that's still missing — here, `Id`, `CustomerId`, `CreatedAtUtc`, and
`Total`. `DisplayOnlySummary` is skipped because it opts out.

3. The action method receives the model back whole on `POST`:

   ```csharp
   [HttpPost]
   public IActionResult Save(OrderEditModel model)
   {
       // model.Id, model.CustomerId, model.CreatedAtUtc, model.Total are all populated.
       return RedirectToAction(nameof(Saved), new { model.Id });
   }
   ```

## How it works

`PersistOnPostbackTagHelper` targets `<form asp-persist-model="...">` and runs
at `Order = int.MaxValue`, so it executes after every other Tag Helper inside
the form (including `asp-for` inputs) has rendered. It reads the form's
buffered child content, calls `HiddenFieldDiffer.GetMissingFields` to find
which eligible paths have no matching rendered input, and appends the result
of `HiddenFieldRenderer.Render` to the form's post-content — immediately before
the closing `</form>` tag that Razor's own `form` tag helper writes.

## Full example

A runnable sample app lives at
[`samples/ProQualEx.ModelSaver.AspNetCore.Sample`](https://github.com/rsotolongo/ModelSaver/tree/main/samples/ProQualEx.ModelSaver.AspNetCore.Sample)
in the repository.

## Links

- Repository: <https://github.com/rsotolongo/ModelSaver>
- Core package (attribute + diffing engine): <https://www.nuget.org/packages/ProQualEx.ModelSaver.Core>
- ASP.NET MVC 5 adapter: <https://www.nuget.org/packages/ProQualEx.ModelSaver.Mvc5>
- License: MIT
