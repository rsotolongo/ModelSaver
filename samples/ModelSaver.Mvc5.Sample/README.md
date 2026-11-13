# ModelSaver.Mvc5.Sample

A classic ASP.NET MVC 5 app demonstrating `ModelSaver.Mvc5`. It mirrors
`ModelSaver.AspNetCore.Sample` exactly, so you can compare the two
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
hosted by IIS / IIS Express — there is no cross-platform `dotnet run`
for it. It is an SDK-style class library, so Visual Studio can't launch it
as a web project on its own; `Properties/launchSettings.json` instead
starts IIS Express against the project folder, and the build writes its
DLLs straight to `bin\` (where IIS Express expects them).

To run it (Windows, IIS Express installed):

1. Open `ModelSaver.sln` in Visual Studio.
2. Set `ModelSaver.Mvc5.Sample` as the startup project and pick the
   **IIS Express** profile.
3. Press F5. It opens `http://localhost:8081/Order/Edit`.

From a shell instead:

```bash
dotnet build samples/ModelSaver.Mvc5.Sample
"C:\Program Files\IIS Express\iisexpress.exe" /path:%CD%\samples\ModelSaver.Mvc5.Sample /port:8081
```
