using System.Reflection;

namespace ProQualEx.ModelSaver.Core.Tests;

/// <summary>
/// Tests for hidden field differ.
/// </summary>
public class HiddenFieldDifferTests
{
    /// <summary>
    /// Get missing fields property already rendered is excluded.
    /// </summary>
    [Fact]
    public void GetMissingFieldsPropertyAlreadyRenderedIsExcluded()
    {
        var model = new FlatModel { Id = 42, Name = "Ada" };
        const string renderedHtml = "<input type=\"text\" name=\"Name\" value=\"Ada\" />";

        IReadOnlyList<PersistField> missing = HiddenFieldDiffer.GetMissingFields(model, renderedHtml);

        PersistField field = Assert.Single(missing);

        Assert.Equal("Id", field.Name);
        Assert.Equal("42", field.Value);
    }

    /// <summary>
    /// Get missing fields nothing rendered returns all eligible fields.
    /// </summary>
    [Fact]
    public void GetMissingFieldsNothingRenderedReturnsAllEligibleFields()
    {
        var model = new FlatModel { Id = 42, Name = "Ada" };

        IReadOnlyList<PersistField> missing = HiddenFieldDiffer.GetMissingFields(model, renderedFormHtml: string.Empty);

        Assert.Equal(2, missing.Count);
    }

    /// <summary>
    /// Get missing fields everything rendered returns empty.
    /// </summary>
    [Fact]
    public void GetMissingFieldsEverythingRenderedReturnsEmpty()
    {
        var model = new FlatModel { Id = 42, Name = "Ada" };
        const string renderedHtml = "<input name=\"Id\" /><input name=\"Name\" />";

        IReadOnlyList<PersistField> missing = HiddenFieldDiffer.GetMissingFields(model, renderedHtml);

        Assert.Empty(missing);
    }

    /// <summary>
    /// Get missing fields nested property uses dotted name for diff.
    /// </summary>
    [Fact]
    public void GetMissingFieldsNestedPropertyUsesDottedNameForDiff()
    {
        var model = new ParentWithNestedModel { Address = new AddressModel { City = "Miami" } };
        const string renderedHtml = "<input name=\"Address.City\" />";

        IReadOnlyList<PersistField> missing = HiddenFieldDiffer.GetMissingFields(model, renderedHtml);

        Assert.Empty(missing);
    }

    /// <summary>
    /// Get missing fields null model throws.
    /// </summary>
    [Fact]
    public void GetMissingFieldsNullModelThrows()
    {
        MethodInfo method = typeof(HiddenFieldDiffer).GetMethod(nameof(HiddenFieldDiffer.GetMissingFields))
            ?? throw new InvalidOperationException("GetMissingFields was not found.");

        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(obj: null, [null, string.Empty]));

        Assert.IsType<ArgumentNullException>(exception.InnerException);
    }
}
