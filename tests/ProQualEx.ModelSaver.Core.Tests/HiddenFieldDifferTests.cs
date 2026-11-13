using System.Reflection;

namespace ProQualEx.ModelSaver.Core.Tests;

public class HiddenFieldDifferTests
{
    [Fact]
    public void GetMissingFieldsPropertyAlreadyRenderedIsExcluded()
    {
        var model = new FlatModel { Id = 42, Name = "Ada" };
        const string renderedHtml = "<input type=\"text\" name=\"Name\" value=\"Ada\" />";

        IReadOnlyList<PersistField> missing = HiddenFieldDiffer.GetMissingFields(model, renderedHtml);

        Assert.Single(missing);
        Assert.Equal("Id", missing[0].Name);
        Assert.Equal("42", missing[0].Value);
    }

    [Fact]
    public void GetMissingFieldsNothingRenderedReturnsAllEligibleFields()
    {
        var model = new FlatModel { Id = 42, Name = "Ada" };

        IReadOnlyList<PersistField> missing = HiddenFieldDiffer.GetMissingFields(model, renderedFormHtml: string.Empty);

        Assert.Equal(2, missing.Count);
    }

    [Fact]
    public void GetMissingFieldsEverythingRenderedReturnsEmpty()
    {
        var model = new FlatModel { Id = 42, Name = "Ada" };
        const string renderedHtml = "<input name=\"Id\" /><input name=\"Name\" />";

        IReadOnlyList<PersistField> missing = HiddenFieldDiffer.GetMissingFields(model, renderedHtml);

        Assert.Empty(missing);
    }

    [Fact]
    public void GetMissingFieldsNestedPropertyUsesDottedNameForDiff()
    {
        var model = new ParentWithNestedModel { Address = new AddressModel { City = "Miami" } };
        const string renderedHtml = "<input name=\"Address.City\" />";

        IReadOnlyList<PersistField> missing = HiddenFieldDiffer.GetMissingFields(model, renderedHtml);

        Assert.Empty(missing);
    }

    [Fact]
    public void GetMissingFieldsNullModelThrows()
    {
        MethodInfo method = typeof(HiddenFieldDiffer).GetMethod(nameof(HiddenFieldDiffer.GetMissingFields))
            ?? throw new InvalidOperationException("GetMissingFields was not found.");

        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(obj: null, [null, string.Empty]));

        Assert.IsType<ArgumentNullException>(exception.InnerException);
    }
}
