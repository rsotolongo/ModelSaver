# ProQualEx.ModelSaver.Mvc5

[![NuGet](https://img.shields.io/nuget/v/ProQualEx.ModelSaver.Mvc5.svg)](https://www.nuget.org/packages/ProQualEx.ModelSaver.Mvc5)

Classic ASP.NET MVC 5 adapter for
[`ProQualEx.ModelSaver`](https://github.com/rsotolongo/ModelSaver): an
`Html.BeginForm`-style helper that appends every eligible model property not
already rendered as a hidden `<input>`, so it comes back intact on the next
postback.

## Install

```bash
dotnet add package ProQualEx.ModelSaver.Mvc5
```

Targets `net48` and depends on `Microsoft.AspNet.Mvc` (classic ASP.NET MVC 5,
`System.Web.Mvc`) — not ASP.NET Core. For ASP.NET Core, use
[`ProQualEx.ModelSaver.AspNetCore`](https://www.nuget.org/packages/ProQualEx.ModelSaver.AspNetCore)
instead.

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

2. Replace `Html.BeginForm` with `Html.BeginPersistedForm` and only render the
   fields the user actually edits:

   ```cshtml
   @model OrderEditModel

   @using (Html.BeginPersistedForm(Model, "Save", "Order"))
   {
       @Html.TextBoxFor(m => m.CustomerName)
       <button type="submit">Save</button>
   }
   ```

   The helper's full signature mirrors `Html.BeginForm`:

   ```csharp
   IDisposable BeginPersistedForm(
       this HtmlHelper htmlHelper,
       object model,
       string actionName,
       string controllerName,
       FormMethod method = FormMethod.Post,
       object? htmlAttributes = null)
   ```

When the `using` block's scope disposes, it diffs the buffered form HTML
against `model`'s eligible properties (see
[`ProQualEx.ModelSaver.Core`](https://www.nuget.org/packages/ProQualEx.ModelSaver.Core)
for exactly which properties qualify, including nested objects via dotted
paths like `ShippingAddress.City`) and splices a hidden `<input>` for every one
that's still missing — here, `Id`, `CustomerId`, `CreatedAtUtc`, and `Total` —
immediately before the form's closing `</form>` tag. `DisplayOnlySummary` is
skipped because it opts out.

3. The action method receives the model back whole on `POST`:

   ```csharp
   [HttpPost]
   public ActionResult Save(OrderEditModel model)
   {
       // model.Id, model.CustomerId, model.CreatedAtUtc, model.Total are all populated.
       return RedirectToAction(nameof(Saved), new { model.Id });
   }
   ```

## How it works

`BeginPersistedForm` swaps the view's `TextWriter` for an in-memory buffer
before calling the real `Html.BeginForm`, so everything written inside the
`using` block — including nested `Html.BeginForm`/partial output — is captured
rather than sent to the response. Disposing the returned scope disposes the
inner `MvcForm` (writing `</form>` into the buffer), restores the original
writer, diffs the buffered HTML against the model, and writes the spliced
result — original markup plus any missing hidden inputs inserted right before
`</form>` — to the real output.

## Full example

A runnable sample app lives at
[`samples/ProQualEx.ModelSaver.Mvc5.Sample`](https://github.com/rsotolongo/ModelSaver/tree/main/samples/ProQualEx.ModelSaver.Mvc5.Sample)
in the repository. It targets IIS Express on Windows — see that sample's own
README for how to run it.

## Links

- Repository: <https://github.com/rsotolongo/ModelSaver>
- Core package (attribute + diffing engine): <https://www.nuget.org/packages/ProQualEx.ModelSaver.Core>
- ASP.NET Core adapter: <https://www.nuget.org/packages/ProQualEx.ModelSaver.AspNetCore>
- License: MIT
