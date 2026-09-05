# ModelSaver: automatic hidden-field persistence for ASP.NET MVC

## Problem

In traditional ASP.NET MVC (both classic MVC 5 and ASP.NET Core MVC), a
model posted back to an action must round-trip every property the action
needs, not just the ones shown as editable inputs. Properties that aren't
rendered as visible form controls (IDs, computed/derived values, foreign
keys, timestamps, etc.) have to be added as hidden fields by hand, one by
one. This is tedious and error-prone: a forgotten hidden field is a silent
data-loss bug that only surfaces on postback.

ModelSaver removes the manual step: properties are marked with an
attribute, and a form-scoped helper automatically emits hidden fields for
every marked property that wasn't already rendered as a visible input —
determined by inspecting the form's actual rendered output, not by
guessing or requiring a parallel wrapper API.

## Scope (v1)

In scope:
- Flat scalar properties (string, numeric types, `bool`, `DateTime`,
  `Guid`, `enum`).
- Nested complex-object properties (e.g. `Model.Address.City`),
  recursively, governed by each nested type's own attribute
  configuration.
- Both ASP.NET MVC 5 (`System.Web.Mvc`) and ASP.NET Core MVC.
- Distribution as NuGet packages.

Explicitly out of scope for v1 (candidates for later extension, not
addressed by this design):
- Collections/arrays of complex types (`List<T>`, indexed paths like
  `Items[0].Id`). Flat/nested-object support should not preclude adding
  this later, but the diff algorithm and path builder in this design only
  produce dotted paths, not indexed ones.
- Tamper protection / signing / encryption of hidden field values. Not
  required for the current use case (no sensitive data crosses through
  hidden fields). If a future model needs this, it should be an opt-in
  addition, not a default behavior.
- Razor Pages handler binding, Blazor.

## Attribute API

One attribute, valid on both class and property, defined in
`ModelSaver.Core`:

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public sealed class PersistOnPostbackAttribute : Attribute
{
    public bool Persist { get; }
    public PersistOnPostbackAttribute(bool persist = true) => Persist = persist;
}
```

Resolution per property, in order:
1. If the property itself carries `[PersistOnPostback]`, its `Persist`
   value wins outright.
2. Otherwise, if the *declaring class* carries `[PersistOnPostback]`, use
   its `Persist` value as the default for all properties on that class.
3. Otherwise, the property does not persist.

This supports all three usage styles:
- **Pure opt-in**: no class attribute; mark individual properties with
  `[PersistOnPostback]`.
- **Opt-out with a safe default**: `[PersistOnPostback]` on the class,
  `[PersistOnPostback(false)]` on the few properties that should be
  excluded (e.g. because they're already rendered as visible inputs and
  don't need attribute-driven handling, or should never round-trip).
- **Mixed**: combine both per class as needed.

For a complex-typed property, the property's own resolved `Persist` value
determines whether ModelSaver descends into it at all. Once inside, the
nested type's members are resolved using the nested type's *own*
class/property attributes — nesting does not inherit the parent's
default. A null complex property is skipped entirely (no hidden fields
for a subtree that has no value; this matches default model-binder
behavior for null nested objects).

## Core diff algorithm (`ModelSaver.Core`)

Framework-agnostic, no dependency on `System.Web.Mvc` or
`Microsoft.AspNetCore.Mvc`. Pure reflection + string processing so it's
fully unit-testable without any ASP.NET hosting.

1. **Build eligible paths.** Walk the model recursively per the attribute
   resolution rules above, producing dotted paths matching ASP.NET's
   model-binding convention (e.g. `Name`, `Address.City`). Each path is
   paired with its current value (via reflection) and its declared CLR
   type.
2. **Extract rendered names.** Scan the HTML that was actually written
   inside the `<form>` for `name="..."` and `name='...'` attributes
   (both quote styles), via a lightweight regex/state-machine scan (not a
   full HTML parser — the input is Razor/Tag-Helper-generated markup, not
   arbitrary HTML). Elements with `type="submit"`, `type="button"`,
   `type="reset"`, or `type="image"` are excluded from this set, so a
   coincidentally-matching button name can't suppress a real hidden
   field.
3. **Diff.** `missing = eligiblePaths − renderedNames`.
4. **Render.** For each missing path, emit:
   `<input type="hidden" name="{name}" id="{id}" value="{value}" />`
   - `name` and `value` are HTML-encoded.
   - `id` is derived from `name` by replacing `.` with `_` (matching the
     convention `Html.HiddenFor` itself uses, so IDs stay consistent with
     any other fields ASP.NET renders for the same model, and don't
     collide with `unobtrusive-validation`/jQuery selector expectations).
   - `value` is formatted with `Convert.ToString(value, CultureInfo.CurrentCulture)`.
     This deliberately matches the formatting convention `Html.HiddenFor`
     and `Html.TextBoxFor` already use, so a property rendered as a
     hidden field via ModelSaver formats identically to how it would have
     formatted had it been rendered explicitly. (Earlier discussion of
     this design considered `InvariantCulture`; that would silently
     diverge from the rest of the form's existing fields and was
     corrected during spec review.)

Calling the form-scoped helper on a model with no `[PersistOnPostback]`
anywhere is a safe no-op (empty eligible-path set, nothing rendered) —
so wrapping a form doesn't require every model to opt in.

## Framework adapters

### `ModelSaver.AspNetCore`

A Tag Helper targeting `<form>` elements carrying a new attribute:

```csharp
[HtmlTargetElement("form", Attributes = "asp-persist-model")]
public class PersistOnPostbackTagHelper : TagHelper
{
    [HtmlAttributeName("asp-persist-model")]
    public object Model { get; set; }

    public override int Order => int.MaxValue; // run after other form-content tag helpers

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = await output.GetChildContentAsync();
        var renderedNames = RenderedNameScanner.ExtractNames(content.GetContent());
        var missing = HiddenFieldDiffer.GetMissingFields(Model, renderedNames);
        output.PostContent.AppendHtml(HiddenFieldRenderer.Render(missing));
    }
}
```

Usage: `<form asp-action="Save" asp-persist-model="Model"> ... </form>`.
No output-buffering trickery is needed — Tag Helpers already provide
rendered child content before the tag closes.

### `ModelSaver.Mvc5`

No Tag Helper equivalent exists in classic MVC, so the adapter uses an
`IDisposable` scope mirroring `Html.BeginForm`:

```csharp
public static IDisposable BeginPersistedForm(
    this HtmlHelper htmlHelper, object model,
    string actionName, string controllerName,
    FormMethod method = FormMethod.Post, object htmlAttributes = null)
```

Mechanics:
1. Swap `ViewContext.Writer` for a `StringWriter`.
2. Delegate to the real `htmlHelper.BeginForm(...)`, which writes the
   opening `<form ...>` tag into the swapped writer and returns an
   `MvcForm` whose `Dispose()` writes the closing `</form>` tag.
3. The caller's `using` block body writes its inputs into the same
   swapped writer.
4. The outer scope's `Dispose()` first disposes the inner `MvcForm`
   (completing the `</form>` tag in the buffer), then runs the diff
   against the fully-buffered `<form>...</form>` string, splices missing
   hidden fields in immediately before the final `</form>`, restores the
   original `ViewContext.Writer`, and writes the finished HTML to it.

This buffers one form's worth of HTML in memory, which is negligible at
form scale.

## Package layout

```
ModelSaver.sln
src/
  ModelSaver.Core/        (netstandard2.0 — attribute, diff algorithm,
                            path builder, name scanner, value formatter,
                            hidden-field HTML renderer)
  ModelSaver.Mvc5/         (net472 — depends on Core + System.Web.Mvc)
  ModelSaver.AspNetCore/   (net6.0+ — depends on Core +
                            Microsoft.AspNetCore.Mvc.Razor)
tests/
  ModelSaver.Core.Tests/
  ModelSaver.Mvc5.Tests/
  ModelSaver.AspNetCore.Tests/
```

Published as three NuGet packages: `ModelSaver.Core`,
`ModelSaver.Mvc5`, `ModelSaver.AspNetCore`.

## Testing strategy

- **`ModelSaver.Core.Tests`** (xUnit, no ASP.NET dependency):
  - Attribute-resolution matrix: no attributes, class-only, property-only,
    both, property override of class default, nested type with its own
    independent attribute configuration.
  - Path building for flat and nested-object models, including a null
    nested-object property (expect it skipped entirely).
  - Name-scanner edge cases: single vs. double quotes, self-closing
    `<input />` vs. `<input>`, excluded `type="submit"/"button"/"reset"/"image"`,
    multiple forms' worth of markup.
  - Value formatting matches `CurrentCulture` conversion for `DateTime`,
    numeric types, `enum`, `Guid`, `bool`, `string`.
- **`ModelSaver.AspNetCore.Tests`**: standard Tag Helper unit-test
  pattern — construct `TagHelperContext`/`TagHelperOutput` directly with
  fixed child content, run `ProcessAsync`, assert the final output HTML
  contains exactly the expected hidden fields and nothing else.
- **`ModelSaver.Mvc5.Tests`**: unit-test the buffer-diff-splice logic
  directly against raw HTML strings representing a fully-rendered
  `<form>...</form>`, bypassing real `HttpContext`/Razor rendering (the
  novel logic here is the splice, not MVC's own form rendering, which is
  already covered by the framework itself).

## Open items for the implementation plan

None outstanding — all decisions above were confirmed during design
review. The writing-plans skill should sequence: Core (attribute +
algorithm + tests) first, since both adapters depend on it, then the two
adapters in parallel-friendly order (they don't depend on each other).
