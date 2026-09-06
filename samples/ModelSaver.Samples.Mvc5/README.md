# ModelSaver.Samples.Mvc5

A classic ASP.NET MVC 5 app demonstrating `ModelSaver.Mvc5`. It mirrors
`ModelSaver.Samples.AspNetCore` exactly, so you can compare the two
adapters side by side.

## What it shows

`Models/OrderEditModel.cs` is marked `[PersistOnPostback]` at the class
level, so every property is a hidden-field candidate except
`DisplayOnlySummary`, which opts out with `[PersistOnPostback(false)]`.

`Views/Order/Edit.cshtml` only renders `CustomerName` as a visible input,
inside `@using (Html.BeginPersistedForm(Model, "Save", "Order")) { ... }`.
When that block's `using` closes, `ModelSaver.Mvc5` diffs the buffered
form HTML against `OrderEditModel`'s eligible properties and splices in
hidden fields for `Id`, `CustomerId`, `CreatedAtUtc`, and `Total` right
before `</form>`.

`Controllers/OrderController.Save` receives the full `OrderEditModel`
back on POST — `Views/Order/Saved.cshtml` echoes every property so you
can see the hidden fields round-tripped correctly, while
`DisplayOnlySummary` comes back empty because it was excluded.

## Running it

This project targets classic ASP.NET MVC 5 (`System.Web.Mvc`), which is
hosted by IIS / IIS Express or Mono — there is no cross-platform
`dotnet run` for it. **On the Linux machine this repo was authored on,
this project was build-verified only** (`dotnet build`, confirming the
C# code compiles against `Microsoft.AspNet.Mvc` 5.2.9 and `ModelSaver.Mvc5`)
— it was never launched or clicked through, unlike
`ModelSaver.Samples.AspNetCore`, which was run end-to-end in a browser.
Classic Razor views (`.cshtml`) are compiled by ASP.NET at request time,
not by `dotnet build`, so the view syntax itself could not be verified
here either — it was written and reviewed carefully against the real
`System.Web.Mvc` / `ModelSaver.Mvc5` APIs, but treat it as unexecuted
until you've run it yourself.

To actually run it:

1. Open `ModelSaver.sln` in Visual Studio on Windows.
2. Set `ModelSaver.Samples.Mvc5` as the startup project.
3. Press F5 (IIS Express). If Visual Studio doesn't offer IIS Express
   automatically for this SDK-style project, run
   `dotnet publish samples/ModelSaver.Samples.Mvc5 -o publish` and point
   IIS or IIS Express at the `publish` folder instead.
