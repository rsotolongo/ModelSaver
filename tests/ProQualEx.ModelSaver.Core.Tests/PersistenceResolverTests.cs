using System.Reflection;
using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

/// <summary>
/// Tests for persistence resolver.
/// </summary>
public class PersistenceResolverTests
{
    /// <summary>
    /// Should persist no attributes anywhere returns false.
    /// </summary>
    [Fact]
    public void ShouldPersistNoAttributesAnywhereReturnsFalse()
    {
        PropertyInfo property = typeof(NoAttributesModel).GetProperty(nameof(NoAttributesModel.Name))!;

        Assert.False(PersistenceResolver.ShouldPersist(property));
    }

    /// <summary>
    /// Should persist class level default true applies to unmarked property.
    /// </summary>
    [Fact]
    public void ShouldPersistClassLevelDefaultTrueAppliesToUnmarkedProperty()
    {
        PropertyInfo property = typeof(ClassDefaultModel).GetProperty(nameof(ClassDefaultModel.IncludedByDefault))!;

        Assert.True(PersistenceResolver.ShouldPersist(property));
    }

    /// <summary>
    /// Should persist property overrides class default to false.
    /// </summary>
    [Fact]
    public void ShouldPersistPropertyOverridesClassDefaultToFalse()
    {
        PropertyInfo property = typeof(ClassDefaultModel).GetProperty(nameof(ClassDefaultModel.ExcludedByOverride))!;

        Assert.False(PersistenceResolver.ShouldPersist(property));
    }

    /// <summary>
    /// Should persist property only attribute returns true for marked property.
    /// </summary>
    [Fact]
    public void ShouldPersistPropertyOnlyAttributeReturnsTrueForMarkedProperty()
    {
        PropertyInfo property = typeof(PropertyOnlyModel).GetProperty(nameof(PropertyOnlyModel.IncludedExplicitly))!;

        Assert.True(PersistenceResolver.ShouldPersist(property));
    }

    /// <summary>
    /// Should persist property only attribute returns false for unmarked property.
    /// </summary>
    [Fact]
    public void ShouldPersistPropertyOnlyAttributeReturnsFalseForUnmarkedProperty()
    {
        PropertyInfo property = typeof(PropertyOnlyModel).GetProperty(nameof(PropertyOnlyModel.NotMarked))!;

        Assert.False(PersistenceResolver.ShouldPersist(property));
    }

    /// <summary>
    /// Should persist class level default false property overrides to true.
    /// </summary>
    [Fact]
    public void ShouldPersistClassLevelDefaultFalsePropertyOverridesToTrue()
    {
        PropertyInfo property = typeof(ClassDefaultFalseModel).GetProperty(nameof(ClassDefaultFalseModel.PersistedByOverride))!;

        Assert.True(PersistenceResolver.ShouldPersist(property));
    }

    /// <summary>
    /// Should persist class level default false unmarked property returns false.
    /// </summary>
    [Fact]
    public void ShouldPersistClassLevelDefaultFalseUnmarkedPropertyReturnsFalse()
    {
        PropertyInfo property = typeof(ClassDefaultFalseModel).GetProperty(nameof(ClassDefaultFalseModel.NotPersistedByDefault))!;

        Assert.False(PersistenceResolver.ShouldPersist(property));
    }
}
