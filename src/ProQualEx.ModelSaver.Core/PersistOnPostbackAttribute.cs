namespace ProQualEx.ModelSaver;

/// <summary>
/// Marks a class or property as persisted (or explicitly not persisted) across form postbacks.
/// </summary>
/// <param name="persist"><see langword="true"/> to persist the member; <see langword="false"/> to exclude it.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public sealed class PersistOnPostbackAttribute(bool persist = true) : Attribute
{
    /// <summary>
    /// Gets a value indicating whether the decorated member is persisted.
    /// </summary>
    public bool Persist { get; } = persist;
}
