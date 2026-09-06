using System.Collections.Generic;
using System.Text.RegularExpressions;
using ModelSaver;

namespace ModelSaver.Core.Tests;

public class HiddenFieldRendererTests
{
    [Fact]
    public void Render_SingleField_ProducesHiddenInputWithNameIdAndValue()
    {
        var fields = new List<PersistField> { new PersistField("Id", "42") };

        var html = HiddenFieldRenderer.Render(fields);

        Assert.Equal("<input type=\"hidden\" name=\"Id\" id=\"Id\" value=\"42\" />", html);
    }

    [Fact]
    public void Render_DottedName_ReplacesDotsWithUnderscoresInId()
    {
        var fields = new List<PersistField> { new PersistField("Address.City", "Miami") };

        var html = HiddenFieldRenderer.Render(fields);

        Assert.Contains("name=\"Address.City\"", html);
        Assert.Contains("id=\"Address_City\"", html);
    }

    [Fact]
    public void Render_ValueWithHtmlSpecialCharacters_IsEncoded()
    {
        var fields = new List<PersistField> { new PersistField("Bio", "<script>alert(1)</script>") };

        var html = HiddenFieldRenderer.Render(fields);

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Render_MultipleFields_ProducesOneInputPerField()
    {
        var fields = new List<PersistField>
        {
            new PersistField("Id", "42"),
            new PersistField("Name", "Ada")
        };

        var html = HiddenFieldRenderer.Render(fields);

        Assert.Equal(2, Regex.Matches(html, "<input").Count);
    }

    [Fact]
    public void Render_EmptyList_ProducesEmptyString()
    {
        Assert.Equal(string.Empty, HiddenFieldRenderer.Render(new List<PersistField>()));
    }
}
