namespace ProQualEx.ModelSaver.AspNetCore.Tests;

[PersistOnPostback]
internal sealed class OrderModel
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;
}
