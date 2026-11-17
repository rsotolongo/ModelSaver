using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

/// <summary>
/// Tests for rendered name scanner.
/// </summary>
public class RenderedNameScannerTests
{
    /// <summary>
    /// Extract names double quoted input is extracted.
    /// </summary>
    [Fact]
    public void ExtractNamesDoubleQuotedInputIsExtracted()
    {
        const string html = "<input type=\"text\" name=\"Name\" value=\"Ada\" />";

        HashSet<string> names = RenderedNameScanner.ExtractNames(html);

        Assert.Contains("Name", names);
    }

    /// <summary>
    /// Extract names single quoted input is extracted.
    /// </summary>
    [Fact]
    public void ExtractNamesSingleQuotedInputIsExtracted()
    {
        const string html = "<input type='text' name='Name' value='Ada' />";

        HashSet<string> names = RenderedNameScanner.ExtractNames(html);

        Assert.Contains("Name", names);
    }

    /// <summary>
    /// Extract names select and textarea are extracted.
    /// </summary>
    [Fact]
    public void ExtractNamesSelectAndTextareaAreExtracted()
    {
        const string html = "<select name=\"Country\"></select><textarea name=\"Bio\"></textarea>";

        HashSet<string> names = RenderedNameScanner.ExtractNames(html);

        Assert.Contains("Country", names);
        Assert.Contains("Bio", names);
    }

    /// <summary>
    /// Extract names excludes non data input types.
    /// </summary>
    [Theory]
    [InlineData("submit")]
    [InlineData("button")]
    [InlineData("reset")]
    [InlineData("image")]
    public void ExtractNamesExcludesNonDataInputTypes(string inputType)
    {
        string html = $"<input type=\"{inputType}\" name=\"Save\" value=\"Save\" />";

        HashSet<string> names = RenderedNameScanner.ExtractNames(html);

        Assert.DoesNotContain("Save", names);
    }

    /// <summary>
    /// Extract names non self closing input tag is extracted.
    /// </summary>
    [Fact]
    public void ExtractNamesNonSelfClosingInputTagIsExtracted()
    {
        const string html = "<input type=\"text\" name=\"Name\" value=\"Ada\">";

        HashSet<string> names = RenderedNameScanner.ExtractNames(html);

        Assert.Contains("Name", names);
    }

    /// <summary>
    /// Extract names multiple elements returns all names.
    /// </summary>
    [Fact]
    public void ExtractNamesMultipleElementsReturnsAllNames()
    {
        const string html = "<input name=\"A\" /><input name=\"B\" />";

        HashSet<string> names = RenderedNameScanner.ExtractNames(html);

        Assert.Equal(2, names.Count);
        Assert.Contains("A", names);
        Assert.Contains("B", names);
    }
}
