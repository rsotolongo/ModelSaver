namespace ProQualEx.ModelSaver;

/// <summary>
/// A name and value pair to be persisted as a hidden form field.
/// </summary>
/// <param name="name">The form field name, using dotted paths for nested properties.</param>
/// <param name="value">The string value to persist.</param>
public sealed class PersistField(string name, string value)
{
    /// <summary>
    /// Gets the form field name.
    /// </summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// Gets the string value to persist.
    /// </summary>
    public string Value { get; } = value ?? throw new ArgumentNullException(nameof(value));
}
