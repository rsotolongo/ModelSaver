using ModelSaver;

namespace ModelSaver.Samples.AspNetCore.Models;

[PersistOnPostback]
public class OrderEditModel
{
    public int Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public decimal Total { get; set; }

    public string CustomerName { get; set; } = "";

    [PersistOnPostback(false)]
    public string DisplayOnlySummary { get; set; } = "";
}
