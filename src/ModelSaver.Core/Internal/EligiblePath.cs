using System;

namespace ModelSaver.Internal;

internal sealed class EligiblePath
{
    public string Name { get; }
    public object? Value { get; }
    public Type ValueType { get; }

    public EligiblePath(string name, object? value, Type valueType)
    {
        Name = name;
        Value = value;
        ValueType = valueType;
    }
}
