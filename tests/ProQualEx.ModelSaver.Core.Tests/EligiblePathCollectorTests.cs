using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

/// <summary>
/// Tests for eligible path collector.
/// </summary>
public class EligiblePathCollectorTests
{
    /// <summary>
    /// Collect flat model returns only eligible scalar properties.
    /// </summary>
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

    /// <summary>
    /// Collect nested complex property builds dotted path.
    /// </summary>
    [Fact]
    public void CollectNestedComplexPropertyBuildsDottedPath()
    {
        var model = new ParentWithNestedModel { Address = new AddressModel { City = "Miami" } };

        IReadOnlyList<EligiblePath> paths = EligiblePathCollector.Collect(model);

        EligiblePath path = Assert.Single(paths);

        Assert.Equal("Address.City", path.Name);
        Assert.Equal("Miami", path.Value);
    }

    /// <summary>
    /// Collect null nested complex property is skipped entirely.
    /// </summary>
    [Fact]
    public void CollectNullNestedComplexPropertyIsSkippedEntirely()
    {
        var model = new ParentWithNestedModel { Address = null };

        IReadOnlyList<EligiblePath> paths = EligiblePathCollector.Collect(model);

        Assert.Empty(paths);
    }

    /// <summary>
    /// Collect nested type uses its own attributes not parent default.
    /// </summary>
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
