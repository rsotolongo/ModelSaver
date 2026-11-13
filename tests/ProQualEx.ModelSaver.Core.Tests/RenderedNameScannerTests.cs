using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

public class RenderedNameScannerTests
{
    [Fact]
    public void ExtractNamesDoubleQuotedInputIsExtracted()
    {
        const string html = "<input type=\"text\" name=\"Name\" value=\"Ada\" />";

        HashSet<string> names = RenderedNameScanner.ExtractNames(html);

        Assert.Contains("Name", names);
    }

    [Fact]
    public void ExtractNamesSingleQuotedInputIsExtracted()
    {
        const string html = "<input type='text' name='Name' value='Ada' />";

        HashSet<string> names = RenderedNameScanner.ExtractNames(html);

        Assert.Contains("Name", names);
    }

    [Fact]
    public void ExtractNamesSelectAndTextareaAreExtracted()
    {
        const string html = "<select name=\"Country\"></select><textarea name=\"Bio\"></textarea>";

        HashSet<string> names = RenderedNameScanner.ExtractNames(html);

        Assert.Contains("Country", names);
        Assert.Contains("Bio", names);
    }

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

    [Fact]
    public void ExtractNamesNonSelfClosingInputTagIsExtracted()
    {
        const string html = "<input type=\"text\" name=\"Name\" value=\"Ada\">";

        HashSet<string> names = RenderedNameScanner.ExtractNames(html);

        Assert.Contains("Name", names);
    }

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
