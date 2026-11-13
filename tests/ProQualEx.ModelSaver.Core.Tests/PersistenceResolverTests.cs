using System.Reflection;
using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

public class PersistenceResolverTests
{
    [Fact]
    public void ShouldPersistNoAttributesAnywhereReturnsFalse()
    {
        PropertyInfo property = typeof(NoAttributesModel).GetProperty(nameof(NoAttributesModel.Name))!;

        Assert.False(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersistClassLevelDefaultTrueAppliesToUnmarkedProperty()
    {
        PropertyInfo property = typeof(ClassDefaultModel).GetProperty(nameof(ClassDefaultModel.IncludedByDefault))!;

        Assert.True(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersistPropertyOverridesClassDefaultToFalse()
    {
        PropertyInfo property = typeof(ClassDefaultModel).GetProperty(nameof(ClassDefaultModel.ExcludedByOverride))!;

        Assert.False(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersistPropertyOnlyAttributeReturnsTrueForMarkedProperty()
    {
        PropertyInfo property = typeof(PropertyOnlyModel).GetProperty(nameof(PropertyOnlyModel.IncludedExplicitly))!;

        Assert.True(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersistPropertyOnlyAttributeReturnsFalseForUnmarkedProperty()
    {
        PropertyInfo property = typeof(PropertyOnlyModel).GetProperty(nameof(PropertyOnlyModel.NotMarked))!;

        Assert.False(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersistClassLevelDefaultFalsePropertyOverridesToTrue()
    {
        PropertyInfo property = typeof(ClassDefaultFalseModel).GetProperty(nameof(ClassDefaultFalseModel.PersistedByOverride))!;

        Assert.True(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersistClassLevelDefaultFalseUnmarkedPropertyReturnsFalse()
    {
        PropertyInfo property = typeof(ClassDefaultFalseModel).GetProperty(nameof(ClassDefaultFalseModel.NotPersistedByDefault))!;

        Assert.False(PersistenceResolver.ShouldPersist(property));
    }
}
