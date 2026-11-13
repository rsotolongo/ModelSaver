namespace ProQualEx.ModelSaver.Core.Tests;

[PersistOnPostback]
internal sealed class FlatModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    [PersistOnPostback(false)]
    public string Ignored { get; set; } = string.Empty;
}
