namespace ProQualEx.ModelSaver.Mvc5.Sample.Models;

[PersistOnPostback]
public class OrderEditModel
{
    public int? Id { get; set; }

    public Guid? CustomerId { get; set; }

    public DateTimeOffset? CreatedAtUtc { get; set; }

    public decimal? Total { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public ShippingAddress ShippingAddress { get; set; } = new();

    [PersistOnPostback(false)]
    public string DisplayOnlySummary { get; set; } = string.Empty;
}
