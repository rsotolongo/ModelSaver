namespace ProQualEx.ModelSaver.Core.Tests;

internal sealed class PropertyOnlyModel
{
    [PersistOnPostback]
    public string IncludedExplicitly { get; set; } = string.Empty;

    public string NotMarked { get; set; } = string.Empty;
}
