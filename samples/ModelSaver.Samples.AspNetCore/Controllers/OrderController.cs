using Microsoft.AspNetCore.Mvc;
using ModelSaver.Samples.AspNetCore.Models;

namespace ModelSaver.Samples.AspNetCore.Controllers;

public class OrderController : Controller
{
    [HttpGet]
    public IActionResult Edit()
    {
        // Stands in for "loaded from the database" - only CustomerName
        // will be shown as an editable field in the view.
        var model = new OrderEditModel
        {
            Id = 42,
            CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CreatedAtUtc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Total = 199.99m,
            CustomerName = "Ada Lovelace",
            DisplayOnlySummary = "This value is [PersistOnPostback(false)] - it will NOT come back."
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult Save(OrderEditModel model)
    {
        return View("Saved", model);
    }
}
