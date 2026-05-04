namespace Chubrik.LatLng64.Tests;

using System;
using System.Globalization;

public class FormatTests
{
    // 60.5 and 30.25 are exactly representable in double; the round-trip produces those exact
    // values, so ToString output is byte-stable.
    private static readonly LatLng64 Sample = new(60.5, 30.25);

    [Fact]
    public void ToString_InvariantCulture_UsesCommaSeparator()
    {
        Assert.Equal("60.5, 30.25",
            Sample.ToString(format: null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ToString_CommaDecimalCulture_UsesSemicolonSeparator()
    {
        var de = CultureInfo.GetCultureInfo("de-DE");
        Assert.Equal("60,5; 30,25", Sample.ToString(format: null, de));
    }

    [Fact]
    public void ToString_CustomFormat_AppliesPerAxis()
    {
        Assert.Equal("60.50, 30.25",
            Sample.ToString("F2", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ToString_NullFormat_StripsTrailingZeros()
    {
        var integer = new LatLng64(60.0, 30.0);
        Assert.Equal("60, 30", integer.ToString(format: null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ToString_EmptyFormat_StripsTrailingZeros()
    {
        var integer = new LatLng64(60.0, 30.0);
        Assert.Equal("60, 30", integer.ToString(format: "", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ToString_LongCustomFormat_FallsBackBeyondStackAllocBuffer()
    {
        // F30 produces 33 chars per axis + ", " = 68 — overflows the 64-char stackalloc on NET7+.
        // Verifies the heap fallback runs and gives identical output across both TFMs.
        var result = Sample.ToString("F30", CultureInfo.InvariantCulture);
        Assert.Equal(
            "60.500000000000000000000000000000, 30.250000000000000000000000000000",
            result);
    }

#if NET
    [Fact]
    public void TryFormat_BufferLargeEnough_Succeeds()
    {
        Span<char> buf = stackalloc char[64];
        Assert.True(Sample.TryFormat(buf, out var written, default, CultureInfo.InvariantCulture));
        Assert.Equal("60.5, 30.25", buf[..written].ToString());
    }

    [Fact]
    public void TryFormat_BufferTooSmallForLatitude_Fails()
    {
        Span<char> buf = stackalloc char[3];
        Assert.False(Sample.TryFormat(buf, out var written, default, CultureInfo.InvariantCulture));
        Assert.Equal(0, written);
    }

    [Fact]
    public void TryFormat_BufferTooSmallForSeparator_Fails()
    {
        // "60.5" fits (4 chars), but ", " (2 chars) does not into remaining 1.
        Span<char> buf = stackalloc char[5];
        Assert.False(Sample.TryFormat(buf, out var written, default, CultureInfo.InvariantCulture));
        Assert.Equal(0, written);
    }

    [Fact]
    public void TryFormat_BufferTooSmallForLongitude_Fails()
    {
        // "60.5" + ", " = 6 chars fit; "30.25" needs 5 more; total 11. Buffer = 8.
        Span<char> buf = stackalloc char[8];
        Assert.False(Sample.TryFormat(buf, out var written, default, CultureInfo.InvariantCulture));
        Assert.Equal(0, written);
    }

    [Fact]
    public void TryFormat_BufferExactlyFits_Succeeds()
    {
        // "60.5, 30.25" is exactly 11 chars — locks down the off-by-one boundary.
        Span<char> buf = stackalloc char[11];
        Assert.True(Sample.TryFormat(buf, out var written, default, CultureInfo.InvariantCulture));
        Assert.Equal(11, written);
        Assert.Equal("60.5, 30.25", buf[..written].ToString());
    }

    [Fact]
    public void TryFormat_CommaDecimalCulture_UsesSemicolonSeparator()
    {
        var de = CultureInfo.GetCultureInfo("de-DE");
        Span<char> buf = stackalloc char[64];
        Assert.True(Sample.TryFormat(buf, out var written, format: default, de));
        Assert.Equal("60,5; 30,25", buf[..written].ToString());
    }

    [Fact]
    public void TryFormat_CustomFormat_AppliesPerAxis()
    {
        Span<char> buf = stackalloc char[64];
        Assert.True(Sample.TryFormat(buf, out var written, "F2".AsSpan(),
            CultureInfo.InvariantCulture));
        Assert.Equal("60.50, 30.25", buf[..written].ToString());
    }

    [Fact]
    public void TryFormatUtf8_BufferLargeEnough_Succeeds()
    {
        Span<byte> buf = stackalloc byte[64];
        Assert.True(Sample.TryFormat(buf, out var written, default, CultureInfo.InvariantCulture));
        Assert.Equal("60.5, 30.25", System.Text.Encoding.UTF8.GetString(buf[..written]));
    }

    [Fact]
    public void TryFormatUtf8_CommaDecimalCulture_UsesSemicolonSeparator()
    {
        var de = CultureInfo.GetCultureInfo("de-DE");
        Span<byte> buf = stackalloc byte[64];
        Assert.True(Sample.TryFormat(buf, out var written, default, de));
        Assert.Equal("60,5; 30,25", System.Text.Encoding.UTF8.GetString(buf[..written]));
    }

    [Fact]
    public void TryFormatUtf8_BufferTooSmall_Fails()
    {
        Span<byte> buf = stackalloc byte[8];
        Assert.False(Sample.TryFormat(buf, out var written, default, CultureInfo.InvariantCulture));
        Assert.Equal(0, written);
    }
#endif
}
