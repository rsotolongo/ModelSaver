using ProQualEx.ModelSaver;

namespace ProQualEx.ModelSaver.Core.Tests;

[PersistOnPostback]
public class FlatModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    [PersistOnPostback(false)]
    public string Ignored { get; set; } = "";
}

[PersistOnPostback]
public class AddressModel
{
    public string City { get; set; } = "";
}

public class NoAttributeAddressModel
{
    public string City { get; set; } = "";
}

public class ParentWithNestedModel
{
    [PersistOnPostback]
    public AddressModel? Address { get; set; }
}

public class ParentWithNestedNoAttributeModel
{
    [PersistOnPostback]
    public NoAttributeAddressModel? Address { get; set; }
}
