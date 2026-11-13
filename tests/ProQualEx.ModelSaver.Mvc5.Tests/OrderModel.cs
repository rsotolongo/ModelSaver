namespace ProQualEx.ModelSaver.Mvc5.Tests;

[PersistOnPostback]
internal sealed class OrderModel
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;
}
