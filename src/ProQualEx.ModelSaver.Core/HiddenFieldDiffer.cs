using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver;

/// <summary>
/// Finds the model values that a rendered form does not already contain as inputs.
/// </summary>
public static class HiddenFieldDiffer
{
    /// <summary>
    /// Returns a hidden field for every persistable model value that has no matching input in the rendered form.
    /// </summary>
    /// <param name="model">The model whose eligible properties are compared.</param>
    /// <param name="renderedFormHtml">The HTML already rendered for the form.</param>
    /// <returns>The fields that still need to be rendered as hidden inputs.</returns>
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

        IReadOnlyList<EligiblePath> eligiblePaths = EligiblePathCollector.Collect(model);
        HashSet<string> renderedNames = RenderedNameScanner.ExtractNames(renderedFormHtml);

        return
        [
            .. eligiblePaths
                .Where(path => !renderedNames.Contains(path.Name))
                .Select(static path => new PersistField(path.Name, PersistValueFormatter.Format(path.Value))),
        ];
    }
}
