namespace Chubrik.LatLng64.Tests;

using System;
using System.Globalization;

[Collection("CultureSensitive")]
public class TextRoundTripTests
{
    // (55.7558, 37.6173) lands on an exact CENTRAL bin (both axes are integral multiples of
    // 1/EXACT_MUL = 5e-8), so ToString → Parse must reproduce Sample byte-for-byte
    // when cultures match.
    private static readonly LatLng64 Sample = new(55.7558, 37.6173);

    [Fact]
    public void RoundTrip_Invariant_Invariant_Succeeds()
    {
        var formatted = Sample.ToString(format: null, CultureInfo.InvariantCulture);
        var parsed = LatLng64.Parse(formatted, CultureInfo.InvariantCulture);
        Assert.Equal(Sample, parsed);
    }

    [Fact]
    public void RoundTrip_DeDe_DeDe_Succeeds()
    {
        var de = CultureInfo.GetCultureInfo("de-DE");
        var formatted = Sample.ToString(format: null, de);
        var parsed = LatLng64.Parse(formatted, de);
        Assert.Equal(Sample, parsed);
    }

    [Fact]
    public void RoundTrip_FormatDeDe_ParseInvariant_FailsWithFormatException()
    {
        // de-DE produces "55,7558; 37,6173"; Invariant Parse expects ',' as pair separator.
        var de = CultureInfo.GetCultureInfo("de-DE");
        var formatted = Sample.ToString(format: null, de);
        Assert.Throws<FormatException>(
            () => LatLng64.Parse(formatted, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RoundTrip_FormatInvariant_ParseDeDe_FailsWithFormatException()
    {
        // Invariant produces "55.7558, 37.6173"; de-DE Parse expects ';' as pair separator.
        var de = CultureInfo.GetCultureInfo("de-DE");
        var formatted = Sample.ToString(format: null, CultureInfo.InvariantCulture);
        Assert.Throws<FormatException>(() => LatLng64.Parse(formatted, de));
    }

    // Pins the post-F1+F3 contract: null provider resolves to CurrentCulture, not Invariant.
    [Fact]
    public void ToString_NullProvider_UsesCurrentCulture()
    {
        WithCurrentCulture(CultureInfo.GetCultureInfo("de-DE"), () =>
            Assert.Equal("55,7558; 37,6173", Sample.ToString(format: null, formatProvider: null)));
    }

    [Fact]
    public void Parse_NullProvider_UsesCurrentCulture()
    {
        WithCurrentCulture(CultureInfo.GetCultureInfo("de-DE"), () =>
            Assert.Equal(Sample, LatLng64.Parse("55,7558; 37,6173", provider: null)));
    }

    [Fact]
    public void RoundTrip_NullNull_SameCurrentCulture_Succeeds()
    {
        WithCurrentCulture(CultureInfo.GetCultureInfo("de-DE"), () =>
        {
            var formatted = Sample.ToString(format: null, formatProvider: null);
            var parsed = LatLng64.Parse(formatted, provider: null);
            Assert.Equal(Sample, parsed);
        });
    }

    // Cross-machine scenario: text was formatted on a de-DE machine, then parsed on an
    // Invariant-culture machine. The post-F1+F3 contract intentionally surfaces this as
    // FormatException rather than silently producing wrong coordinates.
    [Fact]
    public void RoundTrip_NullNull_DifferentCurrentCulture_FailsLoudly()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var formatted = Sample.ToString(format: null, formatProvider: null);

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.Throws<FormatException>(() => LatLng64.Parse(formatted, provider: null));
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    private static void WithCurrentCulture(CultureInfo culture, Action action)
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }
}
