using System;
using System.Globalization;
using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

public class PersistValueFormatterTests
{
    [Fact]
    public void Format_Null_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, PersistValueFormatter.Format(null));
    }

    [Fact]
    public void Format_String_ReturnsSameString()
    {
        Assert.Equal("Ada", PersistValueFormatter.Format("Ada"));
    }

    [Fact]
    public void Format_Int_ReturnsDigits()
    {
        Assert.Equal("42", PersistValueFormatter.Format(42));
    }

    [Fact]
    public void Format_Bool_ReturnsTrueOrFalseText()
    {
        Assert.Equal("True", PersistValueFormatter.Format(true));
    }

    [Fact]
    public void Format_Guid_ReturnsGuidString()
    {
        var guid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Assert.Equal(guid.ToString(), PersistValueFormatter.Format(guid));
    }

    [Fact]
    public void Format_DateTime_MatchesCurrentCultureConversion()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            var date = new DateTime(2026, 3, 5);

            var expected = Convert.ToString(date, CultureInfo.CurrentCulture);
            Assert.Equal(expected, PersistValueFormatter.Format(date));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
