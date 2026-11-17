namespace ProQualEx.ModelSaver.Mvc5.Sample.Models;

/// <summary>
/// A nested shipping address used to show that nested properties are persisted automatically.
/// </summary>
[PersistOnPostback]
public class ShippingAddress
{
    /// <summary>
    /// Gets or sets the street line.
    /// </summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the city, rendered as an editable input.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the postal code, which is persisted as a hidden field.
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the geographic location, persisted as nested hidden fields.
    /// </summary>
    public GeoPoint Location { get; set; } = new();

    /// <summary>
    /// Gets or sets the delivery instructions, which are excluded from the postback.
    /// </summary>
    [PersistOnPostback(false)]
    public string DeliveryInstructions { get; set; } = string.Empty;
}
