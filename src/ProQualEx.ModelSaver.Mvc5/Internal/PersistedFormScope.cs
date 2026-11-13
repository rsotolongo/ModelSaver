using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace ProQualEx.ModelSaver.Mvc5.Internal;

internal sealed class PersistedFormScope : IDisposable
{
    private readonly ViewContext _viewContext;
    private readonly TextWriter _originalWriter;
    private readonly StringWriter _buffer;
    private readonly MvcForm _innerForm;
    private readonly object _model;
    private readonly Stack<TextWriter>? _outputStack;
    private bool _disposed;

    public PersistedFormScope(
        ViewContext viewContext,
        TextWriter originalWriter,
        StringWriter buffer,
        MvcForm innerForm,
        object model,
        Stack<TextWriter>? outputStack)
    {
        _viewContext = viewContext;
        _originalWriter = originalWriter;
        _buffer = buffer;
        _innerForm = innerForm;
        _model = model;
        _outputStack = outputStack;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _innerForm.Dispose();

        var formHtml = _buffer.ToString();
        var spliced = FormHtmlSplicer.Splice(formHtml, _model);

        _outputStack?.Pop();
        _viewContext.Writer = _originalWriter;
        _originalWriter.Write(spliced);
    }
}
