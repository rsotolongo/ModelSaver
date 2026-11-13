using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.WebPages;
using ProQualEx.ModelSaver.Mvc5.Internal;

namespace ProQualEx.ModelSaver.Mvc5;

/// <summary>
/// HTML helper extensions that render a form with automatic hidden-field persistence.
/// </summary>
public static class PersistedFormExtensions
{
    /// <summary>
    /// Begins a form like the standard BeginForm helper, then adds hidden inputs for persistable model values not rendered inside it.
    /// </summary>
    /// <param name="htmlHelper">The HTML helper instance.</param>
    /// <param name="model">The model whose persistable properties are added as hidden inputs.</param>
    /// <param name="actionName">The name of the action method.</param>
    /// <param name="controllerName">The name of the controller.</param>
    /// <param name="method">The HTTP method for the form.</param>
    /// <param name="htmlAttributes">An object that contains the HTML attributes to set for the form element.</param>
    /// <returns>A scope that finishes the form and appends the hidden inputs when disposed.</returns>
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

        ViewContext viewContext = htmlHelper.ViewContext;
        TextWriter originalWriter = viewContext.Writer;
        var buffer = new StringWriter();
        viewContext.Writer = buffer;

        Stack<TextWriter>? outputStack = (htmlHelper.ViewDataContainer as WebPageBase)?.OutputStack;
        outputStack?.Push(buffer);

        MvcForm innerForm = htmlHelper.BeginForm(actionName, controllerName, method, htmlAttributes);

        TextWriter RestoreWriter()
        {
            outputStack?.Pop();
            viewContext.Writer = originalWriter;
            return originalWriter;
        }

        return new PersistedFormScope(buffer, innerForm, model, RestoreWriter);
    }
}
