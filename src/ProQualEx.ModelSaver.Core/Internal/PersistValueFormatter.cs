using System;
using System.Globalization;

namespace ProQualEx.ModelSaver.Internal;

internal static class PersistValueFormatter
{
    public static string Format(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
    }
}
