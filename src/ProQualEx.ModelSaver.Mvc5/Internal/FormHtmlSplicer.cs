namespace ProQualEx.ModelSaver.Mvc5.Internal;

internal static class FormHtmlSplicer
{
    internal static string Splice(string formHtml, object model)
    {
        IReadOnlyList<PersistField> missingFields = HiddenFieldDiffer.GetMissingFields(model, formHtml);
        string hiddenFieldsHtml = HiddenFieldRenderer.Render(missingFields);
        string result = formHtml;

        if (hiddenFieldsHtml.Length > 0)
        {
            int closingTagIndex = formHtml.LastIndexOf("</form>", StringComparison.OrdinalIgnoreCase);

            result = closingTagIndex < 0
                ? formHtml + hiddenFieldsHtml
                : formHtml.Insert(closingTagIndex, hiddenFieldsHtml);
        }

        return result;
    }
}
