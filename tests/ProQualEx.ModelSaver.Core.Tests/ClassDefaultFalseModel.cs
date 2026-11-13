namespace ProQualEx.ModelSaver.Core.Tests;

[PersistOnPostback(false)]
internal sealed class ClassDefaultFalseModel
{
    public string NotPersistedByDefault { get; set; } = string.Empty;

    [PersistOnPostback]
    public string PersistedByOverride { get; set; } = string.Empty;
}
