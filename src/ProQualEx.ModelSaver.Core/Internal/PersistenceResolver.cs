using System.Reflection;

namespace ProQualEx.ModelSaver.Internal;

internal static class PersistenceResolver
{
    internal static bool ShouldPersist(PropertyInfo property)
    {
        if (property is null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        PersistOnPostbackAttribute? effective =
            property.GetCustomAttribute<PersistOnPostbackAttribute>(inherit: true)
            ?? property.DeclaringType?.GetCustomAttribute<PersistOnPostbackAttribute>(inherit: true);

        return effective?.Persist ?? false;
    }
}
