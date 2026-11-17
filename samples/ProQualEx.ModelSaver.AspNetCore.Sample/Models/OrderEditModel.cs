namespace ProQualEx.ModelSaver.AspNetCore.Sample.Models;

/// <summary>
/// The order being edited; only some properties are rendered as inputs and the rest round-trip as hidden fields.
/// </summary>
[PersistOnPostback]
public class OrderEditModel
{
    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// Gets or sets the customer identifier.
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the creation instant in UTC.
    /// </summary>
    public DateTimeOffset? CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the order total.
    /// </summary>
    public decimal? Total { get; set; }

    /// <summary>
    /// Gets or sets the customer name, rendered as an editable input.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the nested shipping address.
    /// </summary>
    public ShippingAddress ShippingAddress { get; set; } = new();

    /// <summary>
    /// Gets or sets a display-only summary, which is excluded from the postback.
    /// </summary>
    [PersistOnPostback(false)]
    public string DisplayOnlySummary { get; set; } = string.Empty;
}
