using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using ModelSaver;
using ModelSaver.AspNetCore;

namespace ModelSaver.AspNetCore.Tests;

public class PersistOnPostbackTagHelperTests
{
    [PersistOnPostback]
    private class OrderModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
    }

    [Fact]
    public async Task ProcessAsync_AppendsHiddenFieldsForMissingProperties()
    {
        var helper = new PersistOnPostbackTagHelper
        {
            Model = new OrderModel { Id = 7, CustomerName = "Ada" }
        };
        var output = CreateOutput("<input name=\"CustomerName\" value=\"Ada\" />");

        await helper.ProcessAsync(CreateContext(), output);

        var rendered = output.PostContent.GetContent();
        Assert.Contains("name=\"Id\"", rendered);
        Assert.DoesNotContain("name=\"CustomerName\"", rendered);
    }

    [Fact]
    public async Task ProcessAsync_EverythingAlreadyRendered_AppendsNothing()
    {
        var helper = new PersistOnPostbackTagHelper
        {
            Model = new OrderModel { Id = 7, CustomerName = "Ada" }
        };
        var output = CreateOutput("<input name=\"Id\" /><input name=\"CustomerName\" />");

        await helper.ProcessAsync(CreateContext(), output);

        Assert.Equal(string.Empty, output.PostContent.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_NullModel_DoesNothing()
    {
        var helper = new PersistOnPostbackTagHelper { Model = null };
        var output = CreateOutput("<input name=\"Id\" />");

        await helper.ProcessAsync(CreateContext(), output);

        Assert.Equal(string.Empty, output.PostContent.GetContent());
    }

    private static TagHelperContext CreateContext() =>
        new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));

    private static TagHelperOutput CreateOutput(string childContent) =>
        new TagHelperOutput(
            "form",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
                Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetHtmlContent(childContent)));
}
