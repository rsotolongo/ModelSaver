using System;
using System.Web.Mvc;
using ProQualEx.ModelSaver.Mvc5.Sample.Models;

namespace ProQualEx.ModelSaver.Mvc5.Sample.Controllers;

public class OrderController : Controller
{
    [HttpGet]
    public ActionResult Edit()
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
            ShippingAddress = new ShippingAddress
            {
                Street = "12 St James's Square",
                City = "London",
                PostalCode = "SW1Y 4JH",
                Location = new GeoPoint { Latitude = 51.5074, Longitude = -0.1278 },
                DeliveryInstructions = "This value is [PersistOnPostback(false)] - it will NOT come back."
            },
            DisplayOnlySummary = "This value is [PersistOnPostback(false)] - it will NOT come back."
        };

        return View(model);
    }

    [HttpPost]
    public ActionResult Save(OrderEditModel model)
    {
        return View("Saved", model);
    }
}
