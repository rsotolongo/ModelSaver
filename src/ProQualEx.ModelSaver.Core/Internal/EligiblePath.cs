namespace ProQualEx.ModelSaver.Internal;

internal sealed class EligiblePath(string name, object? value, Type valueType)
{
    internal string Name { get; } = name;

    internal object? Value { get; } = value;

    internal Type ValueType { get; } = valueType;
}
