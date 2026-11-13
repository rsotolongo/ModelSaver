using System;

namespace ProQualEx.ModelSaver;

public sealed class PersistField
{
    public string Name { get; }
    public string Value { get; }

    public PersistField(string name, string value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}
