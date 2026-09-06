using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ModelSaver.Internal;

internal static class EligiblePathCollector
{
    public static IReadOnlyList<EligiblePath> Collect(object model)
    {
        var results = new List<EligiblePath>();
        CollectInto(model, prefix: "", results);
        return results;
    }

    private static void CollectInto(object model, string prefix, List<EligiblePath> results)
    {
        var properties = model.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        foreach (var property in properties)
        {
            if (!PersistenceResolver.ShouldPersist(property))
            {
                continue;
            }

            var name = prefix.Length == 0 ? property.Name : $"{prefix}.{property.Name}";
            var value = property.GetValue(model);

            if (ScalarTypes.IsScalar(property.PropertyType))
            {
                results.Add(new EligiblePath(name, value, property.PropertyType));
            }
            else if (value is not null)
            {
                CollectInto(value, name, results);
            }
        }
    }
}
