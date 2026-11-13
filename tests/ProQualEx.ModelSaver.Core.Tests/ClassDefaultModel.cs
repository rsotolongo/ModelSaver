namespace ProQualEx.ModelSaver.Core.Tests;

[PersistOnPostback]
internal sealed class ClassDefaultModel
{
    public string IncludedByDefault { get; set; } = string.Empty;

    [PersistOnPostback(false)]
    public string ExcludedByOverride { get; set; } = string.Empty;
}
