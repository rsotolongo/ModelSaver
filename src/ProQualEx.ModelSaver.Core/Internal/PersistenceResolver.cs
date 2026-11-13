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

        PersistOnPostbackAttribute? propertyAttribute = property.GetCustomAttribute<PersistOnPostbackAttribute>(inherit: true);
        if (propertyAttribute is not null)
        {
            return propertyAttribute.Persist;
        }

        PersistOnPostbackAttribute? classAttribute = property.DeclaringType?.GetCustomAttribute<PersistOnPostbackAttribute>(inherit: true);
        return classAttribute?.Persist ?? false;
    }
}
