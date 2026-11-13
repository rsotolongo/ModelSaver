using System;
using ProQualEx.ModelSaver;
using ProQualEx.ModelSaver.Mvc5.Internal;

namespace ProQualEx.ModelSaver.Mvc5.Tests;

public class FormHtmlSplicerTests
{
    [PersistOnPostback]
    private class OrderModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
    }

    [Fact]
    public void Splice_InsertsMissingHiddenFieldsBeforeClosingFormTag()
    {
        var model = new OrderModel { Id = 7, CustomerName = "Ada" };
        var formHtml = "<form action=\"/Order/Save\" method=\"post\"><input name=\"CustomerName\" value=\"Ada\" /></form>";

        var result = FormHtmlSplicer.Splice(formHtml, model);

        var closingTagIndex = result.IndexOf("</form>", StringComparison.Ordinal);
        var hiddenFieldIndex = result.IndexOf("name=\"Id\"", StringComparison.Ordinal);
        Assert.True(hiddenFieldIndex > 0);
        Assert.True(hiddenFieldIndex < closingTagIndex);
    }

    [Fact]
    public void Splice_EverythingAlreadyRendered_LeavesHtmlUnchanged()
    {
        var model = new OrderModel { Id = 7, CustomerName = "Ada" };
        var formHtml = "<form><input name=\"Id\" /><input name=\"CustomerName\" /></form>";

        var result = FormHtmlSplicer.Splice(formHtml, model);

        Assert.Equal(formHtml, result);
    }

    [Fact]
    public void Splice_NoClosingFormTag_AppendsToEnd()
    {
        var model = new OrderModel { Id = 7, CustomerName = "Ada" };
        var formHtml = "<form>";

        var result = FormHtmlSplicer.Splice(formHtml, model);

        Assert.StartsWith("<form>", result, StringComparison.Ordinal);
        Assert.Contains("name=\"Id\"", result);
        Assert.Contains("name=\"CustomerName\"", result);
    }
}
