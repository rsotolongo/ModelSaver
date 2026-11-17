namespace ProQualEx.ModelSaver.Core.Tests;

/// <summary>
/// Tests for hidden field renderer.
/// </summary>
public class HiddenFieldRendererTests
{
    /// <summary>
    /// Render single field produces hidden input with name id and value.
    /// </summary>
    [Fact]
    public void RenderSingleFieldProducesHiddenInputWithNameIdAndValue()
    {
        List<PersistField> fields = [new("Id", "42")];

        string html = HiddenFieldRenderer.Render(fields);

        Assert.Equal("<input type=\"hidden\" name=\"Id\" id=\"Id\" value=\"42\" />", html);
    }

    /// <summary>
    /// Render dotted name replaces dots with underscores in id.
    /// </summary>
    [Fact]
    public void RenderDottedNameReplacesDotsWithUnderscoresInId()
    {
        List<PersistField> fields = [new("Address.City", "Miami")];

        string html = HiddenFieldRenderer.Render(fields);

        Assert.Contains("name=\"Address.City\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"Address_City\"", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Render value with html special characters is encoded.
    /// </summary>
    [Fact]
    public void RenderValueWithHtmlSpecialCharactersIsEncoded()
    {
        List<PersistField> fields = [new("Bio", "<script>alert(1)</script>")];

        string html = HiddenFieldRenderer.Render(fields);

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Render multiple fields produces one input per field.
    /// </summary>
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

    /// <summary>
    /// Render empty list produces empty string.
    /// </summary>
    [Fact]
    public void RenderEmptyListProducesEmptyString()
    {
        Assert.Equal(string.Empty, HiddenFieldRenderer.Render([]));
    }
}
