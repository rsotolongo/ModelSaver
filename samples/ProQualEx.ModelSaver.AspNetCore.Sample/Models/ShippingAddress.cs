namespace ProQualEx.ModelSaver.AspNetCore.Sample.Models;

[PersistOnPostback]
public class ShippingAddress
{
    public string Street { get; set; } = "";

    public string City { get; set; } = "";

    public string PostalCode { get; set; } = "";

    public GeoPoint Location { get; set; } = new();

    [PersistOnPostback(false)]
    public string DeliveryInstructions { get; set; } = "";
}
