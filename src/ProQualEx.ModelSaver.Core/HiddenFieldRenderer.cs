using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ProQualEx.ModelSaver;

public static class HiddenFieldRenderer
{
    public static string Render(IEnumerable<PersistField> fields)
    {
        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        var builder = new StringBuilder();

        foreach (var field in fields)
        {
            var id = field.Name.Replace('.', '_');

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
