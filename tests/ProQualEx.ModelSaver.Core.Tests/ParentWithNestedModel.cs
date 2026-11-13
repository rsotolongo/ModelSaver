namespace ProQualEx.ModelSaver.Core.Tests;

internal sealed class ParentWithNestedModel
{
    [PersistOnPostback]
    public AddressModel? Address { get; set; }
}
