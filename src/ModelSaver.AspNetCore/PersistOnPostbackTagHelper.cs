using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ModelSaver.AspNetCore;

[HtmlTargetElement("form", Attributes = "asp-persist-model")]
public class PersistOnPostbackTagHelper : TagHelper
{
    [HtmlAttributeName("asp-persist-model")]
    public object? Model { get; set; }

    public override int Order => int.MaxValue;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (Model is null)
        {
            return;
        }

        var content = await output.GetChildContentAsync();
        var renderedHtml = content.GetContent();
        var missingFields = HiddenFieldDiffer.GetMissingFields(Model, renderedHtml);

        if (missingFields.Count > 0)
        {
            output.PostContent.AppendHtml(HiddenFieldRenderer.Render(missingFields));
        }
    }
}
