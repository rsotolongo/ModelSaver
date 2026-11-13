namespace ProQualEx.ModelSaver.Internal;

internal static class ScalarTypes
{
    private static readonly HashSet<Type> ValueLikeTypes =
    [
        typeof(string),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
    ];

    internal static bool IsScalar(Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying.IsPrimitive || underlying.IsEnum || ValueLikeTypes.Contains(underlying);
    }
}
