using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using ProQualEx.ModelSaver.AspNetCore;

namespace ProQualEx.ModelSaver.AspNetCore.Tests;

public class PersistOnPostbackTagHelperTests
{
    [Fact]
    public async Task ProcessAsyncAppendsHiddenFieldsForMissingPropertiesAsync()
    {
        var helper = new PersistOnPostbackTagHelper
        {
            Model = new OrderModel { Id = 7, CustomerName = "Ada" },
        };
        TagHelperOutput output = CreateOutput("<input name=\"CustomerName\" value=\"Ada\" />");

        await helper.ProcessAsync(CreateContext(), output).ConfigureAwait(true);

        string rendered = output.PostContent.GetContent();
        Assert.Contains("name=\"Id\"", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"CustomerName\"", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessAsyncEverythingAlreadyRenderedAppendsNothingAsync()
    {
        var helper = new PersistOnPostbackTagHelper
        {
            Model = new OrderModel { Id = 7, CustomerName = "Ada" },
        };
        TagHelperOutput output = CreateOutput("<input name=\"Id\" /><input name=\"CustomerName\" />");

        await helper.ProcessAsync(CreateContext(), output).ConfigureAwait(true);

        Assert.Equal(string.Empty, output.PostContent.GetContent());
    }

    [Fact]
    public async Task ProcessAsyncNullModelDoesNothingAsync()
    {
        var helper = new PersistOnPostbackTagHelper { Model = null };
        TagHelperOutput output = CreateOutput("<input name=\"Id\" />");

        await helper.ProcessAsync(CreateContext(), output).ConfigureAwait(true);

        Assert.Equal(string.Empty, output.PostContent.GetContent());
    }

    private static TagHelperContext CreateContext()
    {
        return new TagHelperContext(
            [],
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
    }

    private static TagHelperOutput CreateOutput(string childContent)
    {
        return new TagHelperOutput(
            "form",
            [],
            (_, _) => Task.FromResult(new DefaultTagHelperContent().SetHtmlContent(childContent)));
    }
}
