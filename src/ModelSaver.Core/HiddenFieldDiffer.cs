using System;
using System.Collections.Generic;
using System.Linq;
using ModelSaver.Internal;

namespace ModelSaver;

public static class HiddenFieldDiffer
{
    public static IReadOnlyList<PersistField> GetMissingFields(object model, string renderedFormHtml)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (renderedFormHtml is null)
        {
            throw new ArgumentNullException(nameof(renderedFormHtml));
        }

        var eligiblePaths = EligiblePathCollector.Collect(model);
        var renderedNames = RenderedNameScanner.ExtractNames(renderedFormHtml);

        return eligiblePaths
            .Where(path => !renderedNames.Contains(path.Name))
            .Select(path => new PersistField(path.Name, PersistValueFormatter.Format(path.Value)))
            .ToList();
    }
}
