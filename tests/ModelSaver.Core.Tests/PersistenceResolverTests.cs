using ModelSaver;
using ModelSaver.Internal;

namespace ModelSaver.Core.Tests;

public class NoAttributesModel
{
    public string Name { get; set; } = "";
}

[PersistOnPostback]
public class ClassDefaultModel
{
    public string IncludedByDefault { get; set; } = "";

    [PersistOnPostback(false)]
    public string ExcludedByOverride { get; set; } = "";
}

public class PropertyOnlyModel
{
    [PersistOnPostback]
    public string IncludedExplicitly { get; set; } = "";

    public string NotMarked { get; set; } = "";
}

[PersistOnPostback(false)]
public class ClassDefaultFalseModel
{
    public string NotPersistedByDefault { get; set; } = "";

    [PersistOnPostback]
    public string PersistedByOverride { get; set; } = "";
}

public class PersistenceResolverTests
{
    [Fact]
    public void ShouldPersist_NoAttributesAnywhere_ReturnsFalse()
    {
        var property = typeof(NoAttributesModel).GetProperty(nameof(NoAttributesModel.Name))!;
        Assert.False(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersist_ClassLevelDefaultTrue_AppliesToUnmarkedProperty()
    {
        var property = typeof(ClassDefaultModel).GetProperty(nameof(ClassDefaultModel.IncludedByDefault))!;
        Assert.True(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersist_PropertyOverridesClassDefault_ToFalse()
    {
        var property = typeof(ClassDefaultModel).GetProperty(nameof(ClassDefaultModel.ExcludedByOverride))!;
        Assert.False(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersist_PropertyOnlyAttribute_ReturnsTrueForMarkedProperty()
    {
        var property = typeof(PropertyOnlyModel).GetProperty(nameof(PropertyOnlyModel.IncludedExplicitly))!;
        Assert.True(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersist_PropertyOnlyAttribute_ReturnsFalseForUnmarkedProperty()
    {
        var property = typeof(PropertyOnlyModel).GetProperty(nameof(PropertyOnlyModel.NotMarked))!;
        Assert.False(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersist_ClassLevelDefaultFalse_PropertyOverridesToTrue()
    {
        var property = typeof(ClassDefaultFalseModel).GetProperty(nameof(ClassDefaultFalseModel.PersistedByOverride))!;
        Assert.True(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersist_ClassLevelDefaultFalse_UnmarkedPropertyReturnsFalse()
    {
        var property = typeof(ClassDefaultFalseModel).GetProperty(nameof(ClassDefaultFalseModel.NotPersistedByDefault))!;
        Assert.False(PersistenceResolver.ShouldPersist(property));
    }
}
