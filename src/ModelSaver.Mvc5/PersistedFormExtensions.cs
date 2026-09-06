using System;
using System.IO;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using ModelSaver.Mvc5.Internal;

namespace ModelSaver.Mvc5;

public static class PersistedFormExtensions
{
    public static IDisposable BeginPersistedForm(
        this HtmlHelper htmlHelper,
        object model,
        string actionName,
        string controllerName,
        FormMethod method = FormMethod.Post,
        object? htmlAttributes = null)
    {
        if (htmlHelper is null)
        {
            throw new ArgumentNullException(nameof(htmlHelper));
        }

        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var viewContext = htmlHelper.ViewContext;
        var originalWriter = viewContext.Writer;
        var buffer = new StringWriter();
        viewContext.Writer = buffer;

        var innerForm = htmlHelper.BeginForm(actionName, controllerName, method, htmlAttributes);

        return new PersistedFormScope(viewContext, originalWriter, buffer, innerForm, model);
    }
}
