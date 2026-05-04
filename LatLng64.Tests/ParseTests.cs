namespace Chubrik.LatLng64.Tests;

using System;
using System.Globalization;

public class ParseTests
{
    [Fact]
    public void Parse_CommaSeparator_WithInvariantCulture()
    {
        var (lat, lng) = LatLng64.Parse("55.7558, 37.6173", CultureInfo.InvariantCulture)
            .GetCoordinates();
        Assert.Equal(55.7558, lat);
        Assert.Equal(37.6173, lng);
    }

    [Fact]
    public void Parse_SemicolonSeparator_WithCommaDecimalCulture()
    {
        var de = CultureInfo.GetCultureInfo("de-DE");
        var (lat, lng) = LatLng64.Parse("55,7558; 37,6173", de).GetCoordinates();
        Assert.Equal(55.7558, lat);
        Assert.Equal(37.6173, lng);
    }

    [Fact]
    public void Parse_NullString_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => LatLng64.Parse((string)null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("55.7558")] // no separator
    [InlineData("foo, bar")]
    [InlineData("55.7558, abc")]
    [InlineData("abc, 37.6173")]
    [InlineData(",")]
    public void Parse_BadFormat_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => LatLng64.Parse(input, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("91, 0")]
    [InlineData("-91, 0")]
    [InlineData("NaN, 0")]
    [InlineData("Infinity, 0")]
    [InlineData("-Infinity, 0")]
    public void Parse_LatitudeOutOfRange_ThrowsOverflow(string input)
    {
        Assert.Throws<OverflowException>(() => LatLng64.Parse(input, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("0, 181")]
    [InlineData("0, -181")]
    [InlineData("0, NaN")]
    [InlineData("0, Infinity")]
    [InlineData("0, -Infinity")]
    public void Parse_LongitudeOutOfRange_ThrowsOverflow(string input)
    {
        Assert.Throws<OverflowException>(() => LatLng64.Parse(input, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void TryParse_ValidInput_ReturnsTrue()
    {
        Assert.True(LatLng64.TryParse("55.7558, 37.6173", CultureInfo.InvariantCulture,
            out var result));
        Assert.Equal(new LatLng64(55.7558, 37.6173), result);
    }

    [Fact]
    public void TryParse_NullString_ReturnsFalseAndDefault()
    {
        Assert.False(LatLng64.TryParse((string?)null, out var result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void TryParse_String_WithCommaCultureProvider_ParsesSemicolonInput()
    {
        var de = CultureInfo.GetCultureInfo("de-DE");
        Assert.True(LatLng64.TryParse("55,7558; 37,6173", de, out var result));
        Assert.Equal(new LatLng64(55.7558, 37.6173), result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a coordinate")]
    [InlineData("91, 0")]
    [InlineData("0, 181")]
    [InlineData("NaN, 0")]
    public void TryParse_InvalidInput_ReturnsFalseAndDefault(string input)
    {
        Assert.False(LatLng64.TryParse(input, CultureInfo.InvariantCulture, out var result));
        Assert.Equal(default, result);
    }

#if NET
    [Fact]
    public void Parse_ReadOnlySpan_Succeeds()
    {
        var result = LatLng64.Parse("55.7558, 37.6173".AsSpan(), CultureInfo.InvariantCulture);
        Assert.Equal(new LatLng64(55.7558, 37.6173), result);
    }

    [Fact]
    public void TryParse_ReadOnlySpan_ReturnsTrue()
    {
        Assert.True(LatLng64.TryParse("55.7558, 37.6173".AsSpan(), CultureInfo.InvariantCulture,
            out var result));
        Assert.Equal(new LatLng64(55.7558, 37.6173), result);
    }

    [Fact]
    public void TryParse_ReadOnlySpan_BadFormat_ReturnsFalse()
    {
        Assert.False(LatLng64.TryParse("not coords".AsSpan(), CultureInfo.InvariantCulture,
            out var result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void TryParse_ReadOnlySpan_WithCommaCultureProvider_ParsesSemicolonInput()
    {
        var de = CultureInfo.GetCultureInfo("de-DE");
        Assert.True(LatLng64.TryParse("55,7558; 37,6173".AsSpan(), de, out var result));
        Assert.Equal(new LatLng64(55.7558, 37.6173), result);
    }

    [Fact]
    public void Parse_Utf8Span_Succeeds()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("55.7558, 37.6173");
        var result = LatLng64.Parse(bytes, CultureInfo.InvariantCulture);
        Assert.Equal(new LatLng64(55.7558, 37.6173), result);
    }

    [Fact]
    public void Parse_Utf8Span_CommaDecimalCulture_ParsesSemicolonInput()
    {
        var de = CultureInfo.GetCultureInfo("de-DE");
        var bytes = System.Text.Encoding.UTF8.GetBytes("55,7558; 37,6173");
        var result = LatLng64.Parse(bytes, de);
        Assert.Equal(new LatLng64(55.7558, 37.6173), result);
    }

    [Fact]
    public void TryParse_Utf8Span_BadFormat_ReturnsFalse()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("not coords");
        Assert.False(LatLng64.TryParse(bytes, CultureInfo.InvariantCulture, out var result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void TryParse_Utf8Span_OutOfRange_ReturnsFalse()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("91, 0");
        Assert.False(LatLng64.TryParse(bytes, CultureInfo.InvariantCulture, out var result));
        Assert.Equal(default, result);
    }

    [Fact]
    public void Utf8Span_RoundTrip_Invariant()
    {
        var original = new LatLng64(55.7558, 37.6173);
        Span<byte> buf = stackalloc byte[64];
        Assert.True(original.TryFormat(buf, out var written, default, CultureInfo.InvariantCulture));
        var parsed = LatLng64.Parse(buf[..written], CultureInfo.InvariantCulture);
        Assert.Equal(original, parsed);
    }
#endif
}
