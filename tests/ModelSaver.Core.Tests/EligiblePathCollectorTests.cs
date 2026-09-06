using ModelSaver.Internal;

namespace ModelSaver.Core.Tests;

public class EligiblePathCollectorTests
{
    [Fact]
    public void Collect_FlatModel_ReturnsOnlyEligibleScalarProperties()
    {
        var model = new FlatModel { Id = 42, Name = "Ada", Ignored = "skip-me" };

        var paths = EligiblePathCollector.Collect(model);

        Assert.Equal(2, paths.Count);
        Assert.Contains(paths, p => p.Name == "Id" && Equals(p.Value, 42));
        Assert.Contains(paths, p => p.Name == "Name" && Equals(p.Value, "Ada"));
        Assert.DoesNotContain(paths, p => p.Name == "Ignored");
    }

    [Fact]
    public void Collect_NestedComplexProperty_BuildsDottedPath()
    {
        var model = new ParentWithNestedModel { Address = new AddressModel { City = "Miami" } };

        var paths = EligiblePathCollector.Collect(model);

        Assert.Single(paths);
        Assert.Equal("Address.City", paths[0].Name);
        Assert.Equal("Miami", paths[0].Value);
    }

    [Fact]
    public void Collect_NullNestedComplexProperty_IsSkippedEntirely()
    {
        var model = new ParentWithNestedModel { Address = null };

        var paths = EligiblePathCollector.Collect(model);

        Assert.Empty(paths);
    }

    [Fact]
    public void Collect_NestedType_UsesItsOwnAttributesNotParentDefault()
    {
        var model = new ParentWithNestedNoAttributeModel
        {
            Address = new NoAttributeAddressModel { City = "Miami" }
        };

        var paths = EligiblePathCollector.Collect(model);

        Assert.Empty(paths);
    }
}
