using System.Web.Mvc.Html;

namespace ProQualEx.ModelSaver.Mvc5.Internal;

internal sealed class PersistedFormScope(
    StringWriter buffer,
    MvcForm innerForm,
    object model,
    Func<TextWriter> restoreWriter) : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        innerForm.Dispose();

        string formHtml = buffer.ToString();
        restoreWriter().Write(FormHtmlSplicer.Splice(formHtml, model));
        buffer.Dispose();
    }
}
