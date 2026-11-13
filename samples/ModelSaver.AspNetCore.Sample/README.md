# ModelSaver.AspNetCore.Sample

A minimal ASP.NET Core MVC app demonstrating `ModelSaver.AspNetCore`.

## What it shows

`Models/OrderEditModel.cs` is marked `[PersistOnPostback]` at the class
level, so every property is a hidden-field candidate except
`DisplayOnlySummary`, which opts out with `[PersistOnPostback(false)]`.

`Views/Order/Edit.cshtml` only renders `CustomerName` as a visible input,
inside a `<form asp-action="Save" asp-persist-model="Model">`. At render
time, the `asp-persist-model` Tag Helper (from `ModelSaver.AspNetCore`)
scans the form's rendered HTML, sees that `Id`, `CustomerId`,
`CreatedAtUtc`, and `Total` are eligible but not already on the form, and
appends them as hidden `<input>` elements automatically.

`Controllers/OrderController.Save` receives the full `OrderEditModel`
back on POST — `Views/Order/Saved.cshtml` echoes every property so you
can see all four hidden fields round-tripped correctly, while
`DisplayOnlySummary` comes back empty because it was excluded.

## Running it

```bash
dotnet run --project samples/ModelSaver.AspNetCore.Sample
```

Then open the URL it prints (defaults to `http://localhost:5247`).
