using System.Net;
using System.Text;

namespace ProQualEx.ModelSaver;

/// <summary>
/// Renders persist fields as HTML hidden inputs.
/// </summary>
public static class HiddenFieldRenderer
{
    /// <summary>
    /// Renders each field as an encoded hidden input element.
    /// </summary>
    /// <param name="fields">The fields to render.</param>
    /// <returns>The concatenated HTML of all hidden inputs.</returns>
    public static string Render(IEnumerable<PersistField> fields)
    {
        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        var builder = new StringBuilder();

        foreach (PersistField field in fields)
        {
            string id = field.Name.Replace('.', '_');

            builder.Append("<input type=\"hidden\" name=\"")
                .Append(WebUtility.HtmlEncode(field.Name))
                .Append("\" id=\"")
                .Append(WebUtility.HtmlEncode(id))
                .Append("\" value=\"")
                .Append(WebUtility.HtmlEncode(field.Value))
                .Append("\" />");
        }

        return builder.ToString();
    }
}
