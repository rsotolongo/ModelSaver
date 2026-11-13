using System.Web.Mvc;
using ProQualEx.ModelSaver.Mvc5.Sample.Models;

namespace ProQualEx.ModelSaver.Mvc5.Sample.Controllers;

[AllowAnonymous]
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
            CustomerId = new Guid(0x11111111, 0x1111, 0x1111, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11),
            CreatedAtUtc = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            Total = 199.99m,
            CustomerName = "Ada Lovelace",
            ShippingAddress = new ShippingAddress
            {
                Street = "12 St James's Square",
                City = "London",
                PostalCode = "SW1Y 4JH",
                Location = new GeoPoint { Latitude = 51.5074, Longitude = -0.1278 },
                DeliveryInstructions = "This value is [PersistOnPostback(false)] - it will NOT come back.",
            },
            DisplayOnlySummary = "This value is [PersistOnPostback(false)] - it will NOT come back.",
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Save(OrderEditModel model)
    {
        return View("Saved", model);
    }
}
