namespace ProQualEx.ModelSaver.Core.Tests;

internal sealed class ParentWithNestedNoAttributeModel
{
    [PersistOnPostback]
    public NoAttributeAddressModel? Address { get; set; }
}
