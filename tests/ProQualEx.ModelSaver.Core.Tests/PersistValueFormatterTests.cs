using System.Globalization;
using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

/// <summary>
/// Tests for persist value formatter.
/// </summary>
public class PersistValueFormatterTests
{
    /// <summary>
    /// Format null returns empty string.
    /// </summary>
    [Fact]
    public void FormatNullReturnsEmptyString()
    {
        Assert.Equal(string.Empty, PersistValueFormatter.Format(value: null));
    }

    /// <summary>
    /// Format string returns same string.
    /// </summary>
    [Fact]
    public void FormatStringReturnsSameString()
    {
        Assert.Equal("Ada", PersistValueFormatter.Format("Ada"));
    }

    /// <summary>
    /// Format int returns digits.
    /// </summary>
    [Fact]
    public void FormatIntReturnsDigits()
    {
        Assert.Equal("42", PersistValueFormatter.Format(42));
    }

    /// <summary>
    /// Format bool returns true or false text.
    /// </summary>
    [Fact]
    public void FormatBoolReturnsTrueOrFalseText()
    {
        Assert.Equal("True", PersistValueFormatter.Format(value: true));
    }

    /// <summary>
    /// Format guid returns guid string.
    /// </summary>
    [Fact]
    public void FormatGuidReturnsGuidString()
    {
        Guid guid = Guid.Empty;

        Assert.Equal(guid.ToString(), PersistValueFormatter.Format(guid));
    }

    /// <summary>
    /// Format date time offset matches current culture conversion.
    /// </summary>
    [Fact]
    public void FormatDateTimeOffsetMatchesCurrentCultureConversion()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            var date = new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero);

            string? expected = Convert.ToString(date, CultureInfo.CurrentCulture);

            Assert.Equal(expected, PersistValueFormatter.Format(date));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
