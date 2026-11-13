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
        if (Model is null)
        {
            return;
        }

        TagHelperContent content = await output.GetChildContentAsync().ConfigureAwait(false);
        string renderedHtml = content.GetContent();
        IReadOnlyList<PersistField> missingFields = HiddenFieldDiffer.GetMissingFields(Model, renderedHtml);

        if (missingFields.Count is 0)
        {
            return;
        }

        output.PostContent.AppendHtml(HiddenFieldRenderer.Render(missingFields));
    }
}
