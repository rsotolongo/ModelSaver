using System.Globalization;
using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

public class PersistValueFormatterTests
{
    [Fact]
    public void FormatNullReturnsEmptyString()
    {
        Assert.Equal(string.Empty, PersistValueFormatter.Format(value: null));
    }

    [Fact]
    public void FormatStringReturnsSameString()
    {
        Assert.Equal("Ada", PersistValueFormatter.Format("Ada"));
    }

    [Fact]
    public void FormatIntReturnsDigits()
    {
        Assert.Equal("42", PersistValueFormatter.Format(42));
    }

    [Fact]
    public void FormatBoolReturnsTrueOrFalseText()
    {
        Assert.Equal("True", PersistValueFormatter.Format(value: true));
    }

    [Fact]
    public void FormatGuidReturnsGuidString()
    {
        Guid guid = Guid.Empty;

        Assert.Equal(guid.ToString(), PersistValueFormatter.Format(guid));
    }

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
