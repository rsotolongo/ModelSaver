namespace ProQualEx.ModelSaver.Core.Tests;

public class HiddenFieldRendererTests
{
    [Fact]
    public void RenderSingleFieldProducesHiddenInputWithNameIdAndValue()
    {
        List<PersistField> fields = [new("Id", "42")];

        string html = HiddenFieldRenderer.Render(fields);

        Assert.Equal("<input type=\"hidden\" name=\"Id\" id=\"Id\" value=\"42\" />", html);
    }

    [Fact]
    public void RenderDottedNameReplacesDotsWithUnderscoresInId()
    {
        List<PersistField> fields = [new("Address.City", "Miami")];

        string html = HiddenFieldRenderer.Render(fields);

        Assert.Contains("name=\"Address.City\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"Address_City\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderValueWithHtmlSpecialCharactersIsEncoded()
    {
        List<PersistField> fields = [new("Bio", "<script>alert(1)</script>")];

        string html = HiddenFieldRenderer.Render(fields);

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMultipleFieldsProducesOneInputPerField()
    {
        List<PersistField> fields =
        [
            new("Id", "42"),
            new("Name", "Ada"),
        ];

        string html = HiddenFieldRenderer.Render(fields);

        Assert.Equal(2, html.Split("<input", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void RenderEmptyListProducesEmptyString()
    {
        Assert.Equal(string.Empty, HiddenFieldRenderer.Render([]));
    }
}
