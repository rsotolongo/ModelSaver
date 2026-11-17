using ProQualEx.ModelSaver.Mvc5.Internal;

namespace ProQualEx.ModelSaver.Mvc5.Tests;

/// <summary>
/// Tests for form html splicer.
/// </summary>
public class FormHtmlSplicerTests
{
    /// <summary>
    /// Splice inserts missing hidden fields before closing form tag.
    /// </summary>
    [Fact]
    public void SpliceInsertsMissingHiddenFieldsBeforeClosingFormTag()
    {
        var model = new OrderModel { Id = 7, CustomerName = "Ada" };
        const string formHtml = "<form action=\"/Order/Save\" method=\"post\"><input name=\"CustomerName\" value=\"Ada\" /></form>";

        string result = FormHtmlSplicer.Splice(formHtml, model);

        int closingTagIndex = result.IndexOf("</form>", StringComparison.Ordinal);
        int hiddenFieldIndex = result.IndexOf("name=\"Id\"", StringComparison.Ordinal);
        Assert.True(hiddenFieldIndex > 0);
        Assert.True(hiddenFieldIndex < closingTagIndex);
    }

    /// <summary>
    /// Splice everything already rendered leaves html unchanged.
    /// </summary>
    [Fact]
    public void SpliceEverythingAlreadyRenderedLeavesHtmlUnchanged()
    {
        var model = new OrderModel { Id = 7, CustomerName = "Ada" };
        const string formHtml = "<form><input name=\"Id\" /><input name=\"CustomerName\" /></form>";

        string result = FormHtmlSplicer.Splice(formHtml, model);

        Assert.Equal(formHtml, result);
    }

    /// <summary>
    /// Splice no closing form tag appends to end.
    /// </summary>
    [Fact]
    public void SpliceNoClosingFormTagAppendsToEnd()
    {
        var model = new OrderModel { Id = 7, CustomerName = "Ada" };
        const string formHtml = "<form>";

        string result = FormHtmlSplicer.Splice(formHtml, model);

        Assert.StartsWith("<form>", result, StringComparison.Ordinal);
        Assert.Contains("name=\"Id\"", result, StringComparison.Ordinal);
        Assert.Contains("name=\"CustomerName\"", result, StringComparison.Ordinal);
    }
}
