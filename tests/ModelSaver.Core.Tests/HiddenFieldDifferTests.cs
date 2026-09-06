using System;
using ModelSaver;

namespace ModelSaver.Core.Tests;

public class HiddenFieldDifferTests
{
    [Fact]
    public void GetMissingFields_PropertyAlreadyRendered_IsExcluded()
    {
        var model = new FlatModel { Id = 42, Name = "Ada" };
        var renderedHtml = "<input type=\"text\" name=\"Name\" value=\"Ada\" />";

        var missing = HiddenFieldDiffer.GetMissingFields(model, renderedHtml);

        Assert.Single(missing);
        Assert.Equal("Id", missing[0].Name);
        Assert.Equal("42", missing[0].Value);
    }

    [Fact]
    public void GetMissingFields_NothingRendered_ReturnsAllEligibleFields()
    {
        var model = new FlatModel { Id = 42, Name = "Ada" };

        var missing = HiddenFieldDiffer.GetMissingFields(model, renderedFormHtml: "");

        Assert.Equal(2, missing.Count);
    }

    [Fact]
    public void GetMissingFields_EverythingRendered_ReturnsEmpty()
    {
        var model = new FlatModel { Id = 42, Name = "Ada" };
        var renderedHtml = "<input name=\"Id\" /><input name=\"Name\" />";

        var missing = HiddenFieldDiffer.GetMissingFields(model, renderedHtml);

        Assert.Empty(missing);
    }

    [Fact]
    public void GetMissingFields_NestedProperty_UsesDottedNameForDiff()
    {
        var model = new ParentWithNestedModel { Address = new AddressModel { City = "Miami" } };
        var renderedHtml = "<input name=\"Address.City\" />";

        var missing = HiddenFieldDiffer.GetMissingFields(model, renderedHtml);

        Assert.Empty(missing);
    }

    [Fact]
    public void GetMissingFields_NullModel_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => HiddenFieldDiffer.GetMissingFields(null!, ""));
    }
}
