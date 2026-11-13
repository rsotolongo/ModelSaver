using System.Reflection;

namespace ProQualEx.ModelSaver.Internal;

internal static class EligiblePathCollector
{
    internal static IReadOnlyList<EligiblePath> Collect(object model)
    {
        List<EligiblePath> results = [];
        CollectInto(model, prefix: string.Empty, results);
        return results;
    }

    private static void CollectInto(object model, string prefix, List<EligiblePath> results)
    {
        IEnumerable<PropertyInfo> properties = model.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(static p => p.CanRead && p.GetIndexParameters().Length is 0);

        foreach (PropertyInfo property in properties)
        {
            if (!PersistenceResolver.ShouldPersist(property))
            {
                continue;
            }

            string name = prefix.Length is 0 ? property.Name : $"{prefix}.{property.Name}";
            object? value = property.GetValue(model);

            if (ScalarTypes.IsScalar(property.PropertyType))
            {
                results.Add(new EligiblePath(name, value, property.PropertyType));
                continue;
            }

            if (value is not null)
            {
                CollectInto(value, name, results);
            }
        }
    }
}
