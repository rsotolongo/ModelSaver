using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

public class EligiblePathCollectorTests
{
    [Fact]
    public void CollectFlatModelReturnsOnlyEligibleScalarProperties()
    {
        var model = new FlatModel { Id = 42, Name = "Ada", Ignored = "skip-me" };

        IReadOnlyList<EligiblePath> paths = EligiblePathCollector.Collect(model);

        Assert.Equal(2, paths.Count);
        Assert.Contains(paths, static p => p.Name is "Id" && Equals(p.Value, 42));
        Assert.Contains(paths, static p => p.Name is "Name" && Equals(p.Value, "Ada"));
        Assert.DoesNotContain(paths, static p => p.Name is "Ignored");
    }

    [Fact]
    public void CollectNestedComplexPropertyBuildsDottedPath()
    {
        var model = new ParentWithNestedModel { Address = new AddressModel { City = "Miami" } };

        IReadOnlyList<EligiblePath> paths = EligiblePathCollector.Collect(model);

        Assert.Single(paths);
        Assert.Equal("Address.City", paths[0].Name);
        Assert.Equal("Miami", paths[0].Value);
    }

    [Fact]
    public void CollectNullNestedComplexPropertyIsSkippedEntirely()
    {
        var model = new ParentWithNestedModel { Address = null };

        IReadOnlyList<EligiblePath> paths = EligiblePathCollector.Collect(model);

        Assert.Empty(paths);
    }

    [Fact]
    public void CollectNestedTypeUsesItsOwnAttributesNotParentDefault()
    {
        var model = new ParentWithNestedNoAttributeModel
        {
            Address = new NoAttributeAddressModel { City = "Miami" },
        };

        IReadOnlyList<EligiblePath> paths = EligiblePathCollector.Collect(model);

        Assert.Empty(paths);
    }
}
