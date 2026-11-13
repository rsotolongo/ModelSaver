using System;
using ModelSaver;

namespace ModelSaver.Mvc5.Sample.Models;

[PersistOnPostback]
public class OrderEditModel
{
    public int Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public decimal Total { get; set; }

    public string CustomerName { get; set; } = "";

    public ShippingAddress ShippingAddress { get; set; } = new();

    [PersistOnPostback(false)]
    public string DisplayOnlySummary { get; set; } = "";
}
