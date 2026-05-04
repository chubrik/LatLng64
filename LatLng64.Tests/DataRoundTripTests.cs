namespace Chubrik.LatLng64.Tests;

public class DataRoundTripTests
{
    // Mirrors LatLng64 internal *_MIN_DATA constants and NORTHERN_MAX_DATA.
    // Source: LatLng64.cs Private Constants region.
    private const ulong SOUTHERN_MIN  =              6_840_000_000ul;
    private const ulong ANTARCTIC_MIN =     18_000_007_200_000_000ul;
    private const ulong INTERIM_MIN   =    468_000_007_200_000_000ul;
    private const ulong CENTRAL_MIN   =    612_000_007_200_000_000ul;
    private const ulong AURORA_MIN    = 17_316_000_000_000_000_000ul;
    private const ulong ARCTIC_MIN    = 18_180_000_000_000_000_000ul;
    private const ulong NORTHERN_MIN  = 18_414_000_000_000_000_000ul;
    private const ulong NORTHERN_MAX  = 18_432_000_000_359_999_999ul;

    private const int SamplesPerZone = 10_000;

    [Fact]
    public void RoundTrip_UniformStrideInEachZone_DataPreserved()
    {
        StrideZone(SOUTHERN_MIN,  ANTARCTIC_MIN - 1);
        StrideZone(ANTARCTIC_MIN, INTERIM_MIN - 1);
        StrideZone(INTERIM_MIN,   CENTRAL_MIN - 1);
        StrideZone(CENTRAL_MIN,   AURORA_MIN - 1);
        StrideZone(AURORA_MIN,    ARCTIC_MIN - 1);
        StrideZone(ARCTIC_MIN,    NORTHERN_MIN - 1);
        StrideZone(NORTHERN_MIN,  NORTHERN_MAX);
    }

    [Theory]
    [InlineData(SOUTHERN_MIN)]
    [InlineData(ANTARCTIC_MIN - 1)]
    [InlineData(ANTARCTIC_MIN)]
    [InlineData(INTERIM_MIN - 1)]
    [InlineData(INTERIM_MIN)]
    [InlineData(CENTRAL_MIN - 1)]
    [InlineData(CENTRAL_MIN)]
    [InlineData(AURORA_MIN - 1)]
    [InlineData(AURORA_MIN)]
    [InlineData(ARCTIC_MIN - 1)]
    [InlineData(ARCTIC_MIN)]
    [InlineData(NORTHERN_MIN - 1)]
    [InlineData(NORTHERN_MIN)]
    [InlineData(NORTHERN_MAX)]
    public void RoundTrip_ZoneEdgeData_DataPreserved(ulong data)
    {
        AssertRoundTrip(data);
    }

    private static void StrideZone(ulong inclusiveMin, ulong inclusiveMax)
    {
        var step = (inclusiveMax - inclusiveMin) / (SamplesPerZone - 1);
        for (var i = 0; i < SamplesPerZone - 1; i++)
            AssertRoundTrip(inclusiveMin + (ulong)i * step);
        AssertRoundTrip(inclusiveMax);
    }

    private static void AssertRoundTrip(ulong data)
    {
        var (lat, lng) = LatLng64.FromData(data).GetCoordinates();
        var reEncoded = new LatLng64(lat, lng);
        Assert.Equal(data, reEncoded.Data);
    }
}
