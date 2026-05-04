namespace Chubrik.LatLng64.Tests;

public class ZoneRoundTripTests
{
    [Fact]
    public void RoundTrip_NorthernZone()
    {
        CheckRough((90.0), 90.0);
        CheckRough((89.99999995), 90.0);
        CheckRough((89.99999995).Dec(), 89.9999999);
        CheckRough((85.00000005), 85.0000001);
        CheckRough((85.00000005).Dec(), 85);
        CheckRough(85.0, 85.0);
        CheckRough((84.99999995), 85.0);
    }

    [Fact]
    public void RoundTrip_ArcticZone()
    {
        CheckSane((84.99999995).Dec(), 84.9999999);
        CheckSane((72.00000005), 72.0000001);
        CheckSane((72.00000005).Dec(), 72.0);
        CheckSane((72.0), 72.0);
        CheckSane((71.999999975), 72.0);
    }

    [Fact]
    public void RoundTrip_AuroraZone()
    {
        CheckGood((71.999999975).Dec(), 71.99999995);
        CheckGood((60.000000025), 60.00000005);
        CheckGood((60.000000025).Dec(), 60.0);
        CheckGood((60.0), 60.0);
        CheckGood((59.999999975).Dec(), 60.0);
    }

    [Fact]
    public void RoundTrip_CentralZone()
    {
        CheckExact((59.999999975).Dec().Dec(), 59.99999995);
        CheckExact((0.000000025), 0.00000005);
        CheckExact((0.000000025).Dec(), 0.0);
        CheckExact((0.0), 0.0);
        CheckExact((-0.0), 0.0);
        CheckExact((-0.000000025).Inc(), 0.0);
        CheckExact((-0.000000025), -0.00000005);
        CheckExact((-55.999999975).Inc().Inc(), -55.99999995);
    }

    [Fact]
    public void RoundTrip_InterimZone()
    {
        CheckGood((-55.999999975).Inc(), -56.0);
        CheckGood((-56.0), -56.0);
        CheckGood((-56.00000005).Inc(), -56.0);
        CheckGood((-56.00000005), -56.0000001);
        CheckGood((-59.99999995).Inc().Inc(), -59.9999999);
    }

    [Fact]
    public void RoundTrip_AntarcticZone()
    {
        CheckSane((-59.99999995).Inc(), -60.0);
        CheckSane((-60.0), -60.0);
        CheckSane((-60.00000005).Inc(), -60.0);
        CheckSane((-60.00000005), -60.0000001);
        CheckSane((-84.99999995).Inc(), -84.9999999);
    }

    [Fact]
    public void RoundTrip_SouthernZone()
    {
        CheckRough((-84.99999995), -85.0);
        CheckRough((-85.0), -85.0);
        CheckRough((-85.00000005).Inc(), -85.0);
        CheckRough((-85.00000005), -85.0000001);
        CheckRough((-89.99999995).Inc(), -89.9999999);
        CheckRough((-89.99999995), -90.0);
        CheckRough((-90.0), -90.0);
    }

    private static void CheckExact(double latIn, double latCheck)
    {
        Check(latIn, latCheck, (180.0), -180.0);
        Check(latIn, latCheck, (179.999999975), -180.0);
        Check(latIn, latCheck, (179.999999975).Dec(), 179.99999995);
        Check(latIn, latCheck, (0.000000025), 0.00000005);
        Check(latIn, latCheck, (0.000000025).Dec(), 0.0);
        Check(latIn, latCheck, (0.0), 0.0);
        Check(latIn, latCheck, (-0.0), 0.0);
        Check(latIn, latCheck, (-0.000000025).Inc(), 0.0);
        Check(latIn, latCheck, (-0.000000025), -0.00000005);
        Check(latIn, latCheck, (-179.999999975).Inc(), -179.99999995);
        Check(latIn, latCheck, (-179.999999975), -180.0);
        Check(latIn, latCheck, (-180.0), -180.0);
    }

    private static void CheckGood(double latIn, double latCheck)
    {
        Check(latIn, latCheck, (180.0), -180.0);
        Check(latIn, latCheck, (179.99999995), -180.0);
        Check(latIn, latCheck, (179.99999995).Dec(), 179.9999999);
        Check(latIn, latCheck, (0.00000005), 0.0000001);
        Check(latIn, latCheck, (0.00000005).Dec(), 0.0);
        Check(latIn, latCheck, (0.0), 0.0);
        Check(latIn, latCheck, (-0.0), 0.0);
        Check(latIn, latCheck, (-0.00000005).Inc(), 0.0);
        Check(latIn, latCheck, (-0.00000005), -0.0000001);
        Check(latIn, latCheck, (-179.99999995).Inc(), -179.9999999);
        Check(latIn, latCheck, (-179.99999995), -180.0);
        Check(latIn, latCheck, (-180.0), -180.0);
    }

    private static void CheckSane(double latIn, double latCheck)
    {
        Check(latIn, latCheck, (180.0), -180.0);
        Check(latIn, latCheck, (179.9999999), -180.0);
        Check(latIn, latCheck, (179.9999999).Dec(), 179.9999998);
        Check(latIn, latCheck, (0.0000001), 0.0000002);
        Check(latIn, latCheck, (0.0000001).Dec(), 0.0);
        Check(latIn, latCheck, (0.0), 0.0);
        Check(latIn, latCheck, (-0.0), 0.0);
        Check(latIn, latCheck, (-0.0000001).Inc(), 0.0);
        Check(latIn, latCheck, (-0.0000001), -0.0000002);
        Check(latIn, latCheck, (-179.9999999).Inc(), -179.9999998);
        Check(latIn, latCheck, (-179.9999999), -180.0);
        Check(latIn, latCheck, (-180.0), -180.0);
    }

    private static void CheckRough(double latIn, double latCheck)
    {
        Check(latIn, latCheck, (180.0), -180.0);
        Check(latIn, latCheck, (179.9999995), -180.0);
        Check(latIn, latCheck, (179.9999995).Dec(), 179.999999);
        Check(latIn, latCheck, (0.0000005), 0.000001);
        Check(latIn, latCheck, (0.0000005).Dec(), 0.0);
        Check(latIn, latCheck, (0.0), 0.0);
        Check(latIn, latCheck, (-0.0), 0.0);
        Check(latIn, latCheck, (-0.0000005).Inc(), 0.0);
        Check(latIn, latCheck, (-0.0000005), -0.000001);
        Check(latIn, latCheck, (-179.9999995).Inc(), -179.999999);
        Check(latIn, latCheck, (-179.9999995), -180.0);
        Check(latIn, latCheck, (-180.0), -180.0);
    }

    // In-zone (not boundary) coordinates: each pair is unambiguously inside one zone
    // and exactly representable in that zone’s grid, so decode equals input. Catches
    // dispatcher routing a middle value to the wrong zone — boundary tests can’t see
    // this because boundaries are shared between adjacent zones.
    [Theory]
    [InlineData(87.5, -45.0)]   // NORTHERN  [85, 90]   lat GOOD_MUL=1e7,  lng ROUGH_MUL=1e6
    [InlineData(78.0, 100.0)]   // ARCTIC    [72, 85)   lat GOOD_MUL=1e7,  lng SANE_MUL=5e6
    [InlineData(65.0, -120.0)]  // AURORA    [60, 72)   lat EXACT_MUL=2e7, lng GOOD_MUL=1e7
    [InlineData(45.0, 12.5)]    // CENTRAL   (-56, 60)  lat EXACT_MUL=2e7, lng EXACT_MUL=2e7
    [InlineData(-58.0, 50.0)]   // INTERIM   [-60, -56] lat GOOD_MUL=1e7,  lng GOOD_MUL=1e7
    [InlineData(-72.5, -100.0)] // ANTARCTIC [-85, -60) lat GOOD_MUL=1e7,  lng SANE_MUL=5e6
    [InlineData(-87.5, 45.0)]   // SOUTHERN  [-90, -85) lat GOOD_MUL=1e7,  lng ROUGH_MUL=1e6
    public void RoundTrip_ZoneMiddle_ExactlyRepresentable(double latIn, double lngIn)
    {
        var (latOut, lngOut) = new LatLng64(latIn, lngIn).GetCoordinates();
        Assert.Equal(latIn, latOut);
        Assert.Equal(lngIn, lngOut);
    }

    private static void Check(double latIn, double latCheck, double lngIn, double lngCheck)
    {
        var (latOut, lngOut) = new LatLng64(latIn, lngIn).GetCoordinates();
        Assert.Equal(latCheck, latOut);
        Assert.Equal(lngCheck, lngOut);
        AssertNonNegativeZero(latOut);
        AssertNonNegativeZero(lngOut);
    }

    private static void AssertNonNegativeZero(double value)
    {
        if (value == 0)
            Assert.False(value.IsNeg());
    }
}
