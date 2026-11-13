using System;

namespace ProQualEx.ModelSaver;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
public sealed class PersistOnPostbackAttribute : Attribute
{
    public bool Persist { get; }

    public PersistOnPostbackAttribute(bool persist = true)
    {
        Persist = persist;
    }
}
