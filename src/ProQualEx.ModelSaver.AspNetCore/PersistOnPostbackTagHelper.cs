using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ProQualEx.ModelSaver.AspNetCore;

/// <summary>
/// Appends hidden inputs for persistable model values that the form does not already render.
/// </summary>
[HtmlTargetElement("form", Attributes = "asp-persist-model")]
public class PersistOnPostbackTagHelper : TagHelper
{
    /// <summary>
    /// Gets or sets the model whose persistable properties are added to the form as hidden inputs.
    /// </summary>
    [HtmlAttributeName("asp-persist-model")]
    public object? Model { get; set; }

    /// <inheritdoc />
    public override int Order => int.MaxValue;

    /// <inheritdoc />
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        string hiddenFieldsHtml = Model is null
            ? string.Empty
            : await BuildHiddenFieldsHtmlAsync(Model, output).ConfigureAwait(false);

        output.PostContent.AppendHtml(hiddenFieldsHtml);
    }

    private static async Task<string> BuildHiddenFieldsHtmlAsync(object model, TagHelperOutput output)
    {
        TagHelperContent content = await output.GetChildContentAsync().ConfigureAwait(false);

        return HiddenFieldRenderer.Render(HiddenFieldDiffer.GetMissingFields(model, content.GetContent()));
    }
}
