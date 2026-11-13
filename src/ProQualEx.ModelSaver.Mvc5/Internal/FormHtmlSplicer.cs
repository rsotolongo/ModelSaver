using System;
using ProQualEx.ModelSaver;

namespace ProQualEx.ModelSaver.Mvc5.Internal;

internal static class FormHtmlSplicer
{
    public static string Splice(string formHtml, object model)
    {
        var missingFields = HiddenFieldDiffer.GetMissingFields(model, formHtml);
        var hiddenFieldsHtml = HiddenFieldRenderer.Render(missingFields);

        if (hiddenFieldsHtml.Length == 0)
        {
            return formHtml;
        }

        var closingTagIndex = formHtml.LastIndexOf("</form>", StringComparison.OrdinalIgnoreCase);

        return closingTagIndex < 0
            ? formHtml + hiddenFieldsHtml
            : formHtml.Insert(closingTagIndex, hiddenFieldsHtml);
    }
}
