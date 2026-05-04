namespace Chubrik.LatLng64.Tests;

using System;

public class ConstructionTests
{
    [Theory]
    [InlineData(91.0)]
    [InlineData(-91.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_InvalidLatitude_Throws(double lat)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LatLng64(lat, 0.0));
    }

    [Theory]
    [InlineData(181.0)]
    [InlineData(-181.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_InvalidLongitude_Throws(double lng)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LatLng64(0.0, lng));
    }

    [Theory]
    [InlineData( 90.0,  180.0,  90.0, -180.0)]
    [InlineData( 90.0, -180.0,  90.0, -180.0)]
    [InlineData( 90.0,    0.0,  90.0,    0.0)]
    [InlineData(-90.0,  180.0, -90.0, -180.0)]
    [InlineData(-90.0, -180.0, -90.0, -180.0)]
    [InlineData(-90.0,    0.0, -90.0,    0.0)]
    [InlineData(  0.0,  180.0,   0.0, -180.0)]
    [InlineData(  0.0, -180.0,   0.0, -180.0)]
    public void Constructor_BoundaryValues_RoundTrip(
        double inLat, double inLng, double expectedLat, double expectedLng)
    {
        var (lat, lng) = new LatLng64(inLat, inLng).GetCoordinates();
        Assert.Equal(expectedLat, lat);
        Assert.Equal(expectedLng, lng);
    }

    [Fact]
    public void FromData_RoundTripsViaConstructor()
    {
        var coord = new LatLng64(55.7558, 37.6173);
        var rebuilt = LatLng64.FromData(coord.Data);
        Assert.Equal(coord, rebuilt);
        Assert.Equal(coord.Data, rebuilt.Data);
    }

    [Fact]
    public void Deconstruct_HappyPath_ReturnsLatitudeAndLongitude()
    {
        var (lat, lng) = new LatLng64(55.7558, 37.6173);
        Assert.Equal(55.7558, lat);
        Assert.Equal(37.6173, lng);
    }

    [Theory]
    [InlineData(0ul)]                          // default state
    [InlineData(1ul)]                          // below SOUTHERN_MIN_DATA
    [InlineData(6_839_999_999ul)]              // SOUTHERN_MIN_DATA - 1
    [InlineData(18_432_000_000_360_000_000ul)] // NORTHERN_MAX_DATA + 1
    [InlineData(ulong.MaxValue)]
    public void FromData_OutOfRange_Throws(ulong data)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LatLng64.FromData(data));
    }

    [Theory]
    [InlineData(6_840_000_000ul)]              // SOUTHERN_MIN_DATA exactly
    [InlineData(18_432_000_000_359_999_999ul)] // NORTHERN_MAX_DATA exactly
    public void FromData_BoundaryData_RoundTrips(ulong data)
    {
        var (lat, lng) = LatLng64.FromData(data).GetCoordinates();
        var reEncoded = new LatLng64(lat, lng);
        Assert.Equal(data, reEncoded.Data);
    }
}
