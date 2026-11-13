using System.Collections.Generic;
using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

public class RenderedNameScannerTests
{
    [Fact]
    public void ExtractNames_DoubleQuotedInput_IsExtracted()
    {
        var html = "<input type=\"text\" name=\"Name\" value=\"Ada\" />";
        var names = RenderedNameScanner.ExtractNames(html);
        Assert.Contains("Name", names);
    }

    [Fact]
    public void ExtractNames_SingleQuotedInput_IsExtracted()
    {
        var html = "<input type='text' name='Name' value='Ada' />";
        var names = RenderedNameScanner.ExtractNames(html);
        Assert.Contains("Name", names);
    }

    [Fact]
    public void ExtractNames_SelectAndTextarea_AreExtracted()
    {
        var html = "<select name=\"Country\"></select><textarea name=\"Bio\"></textarea>";
        var names = RenderedNameScanner.ExtractNames(html);
        Assert.Contains("Country", names);
        Assert.Contains("Bio", names);
    }

    [Theory]
    [InlineData("submit")]
    [InlineData("button")]
    [InlineData("reset")]
    [InlineData("image")]
    public void ExtractNames_ExcludesNonDataInputTypes(string inputType)
    {
        var html = $"<input type=\"{inputType}\" name=\"Save\" value=\"Save\" />";
        var names = RenderedNameScanner.ExtractNames(html);
        Assert.DoesNotContain("Save", names);
    }

    [Fact]
    public void ExtractNames_NonSelfClosingInputTag_IsExtracted()
    {
        var html = "<input type=\"text\" name=\"Name\" value=\"Ada\">";
        var names = RenderedNameScanner.ExtractNames(html);
        Assert.Contains("Name", names);
    }

    [Fact]
    public void ExtractNames_MultipleElements_ReturnsAllNames()
    {
        var html = "<input name=\"A\" /><input name=\"B\" />";
        var names = RenderedNameScanner.ExtractNames(html);
        Assert.Equal(new HashSet<string> { "A", "B" }, names);
    }
}
