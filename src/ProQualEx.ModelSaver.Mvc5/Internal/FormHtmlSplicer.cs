namespace ProQualEx.ModelSaver.Mvc5.Internal;

internal static class FormHtmlSplicer
{
    internal static string Splice(string formHtml, object model)
    {
        IReadOnlyList<PersistField> missingFields = HiddenFieldDiffer.GetMissingFields(model, formHtml);
        string hiddenFieldsHtml = HiddenFieldRenderer.Render(missingFields);

        if (hiddenFieldsHtml.Length is 0)
        {
            return formHtml;
        }

        int closingTagIndex = formHtml.LastIndexOf("</form>", StringComparison.OrdinalIgnoreCase);

        return closingTagIndex < 0
            ? formHtml + hiddenFieldsHtml
            : formHtml.Insert(closingTagIndex, hiddenFieldsHtml);
    }
}
