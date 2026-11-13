namespace ProQualEx.ModelSaver.Mvc5.Sample.Models;

[PersistOnPostback]
public class ShippingAddress
{
    public string Street { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public GeoPoint Location { get; set; } = new();

    [PersistOnPostback(false)]
    public string DeliveryInstructions { get; set; } = string.Empty;
}
