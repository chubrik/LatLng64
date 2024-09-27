using Chubrik.LatLng64;

// Northern
CheckRough(90.0, 90.0);
CheckRough(89.99999995, 90.0);
CheckRough((89.99999995).Dec(), 89.9999999);
CheckRough((85.00000005).Inc(), 85.0000001);
CheckRough(85.00000005, 85.0);
CheckRough(85.0, 85.0);
CheckRough(84.99999995, 85.0);

// Arctic
CheckSane((84.99999995).Dec(), 84.9999999);
CheckSane((72.00000005).Inc(), 72.0000001);
CheckSane(72.00000005, 72.0);
CheckSane(72.0, 72.0);

// Aurora
CheckGood((71.999999975).Dec(), 71.99999995);
CheckGood((60.000000025).Inc().Inc(), 60.00000005);
CheckGood((60.000000025).Inc(), 60.0);
CheckGood(60.0, 60.0);
CheckGood((59.999999975).Dec(), 60.0);

// Central
CheckExact((59.999999975).Dec().Dec(), 59.99999995);
CheckExact((0.000000025).Inc().Inc(), 0.00000005);
CheckExact((0.000000025).Inc(), 0.0);
CheckExact(0.0, 0.0);
CheckExact(-0.0, 0.0);
CheckExact((-0.000000025).Dec(), 0.0);
CheckExact((-0.000000025).Dec().Dec(), -0.00000005);
CheckExact((-55.999999975).Inc().Inc(), -55.99999995);

// Interim
CheckGood((-55.999999975).Inc(), -56.0);
CheckGood(-56.0, -56.0);
CheckGood((-56.00000005).Dec(), -56.0);
CheckGood((-56.00000005).Dec().Dec(), -56.0000001);
CheckGood((-59.99999995).Inc().Inc(), -59.9999999);

// Antarctic
CheckSane((-59.99999995).Inc(), -60.0);
CheckSane(-60.0, -60.0);
CheckSane((-60.00000005).Dec(), -60.0);
CheckSane((-60.00000005).Dec().Dec(), -60.0000001);
CheckSane((-84.99999995).Inc(), -84.9999999);

// Southern
CheckRough(-84.99999995, -85.0);
CheckRough(-85.0, -85.0);
CheckRough(-85.00000005, -85.0);
CheckRough((-85.00000005).Dec(), -85.0000001);
CheckRough((-89.99999995).Inc(), -89.9999999);
CheckRough(-89.99999995, -90.0);
CheckRough(-90.0, -90.0);

Console.WriteLine("G`All tests passed.");
return;

void CheckExact(double latIn, double latCheck)
{
    Check(latIn, latCheck, -180.0, -180.0);
    Check(latIn, latCheck, -179.999999975, -180.0);
    Check(latIn, latCheck, (-179.999999975).Inc(), -179.99999995);
    Check(latIn, latCheck, (-0.000000025).Dec().Dec(), -0.00000005);
    Check(latIn, latCheck, (-0.000000025).Dec(), 0.0);
    Check(latIn, latCheck, -0.0, 0.0);
    Check(latIn, latCheck, 0.0, 0.0);
    Check(latIn, latCheck, (0.000000025).Inc(), 0.0);
    Check(latIn, latCheck, (0.000000025).Inc().Inc(), 0.00000005);
    Check(latIn, latCheck, (179.999999975).Dec(), 179.99999995);
    Check(latIn, latCheck, 179.999999975, -180.0);
    Check(latIn, latCheck, 180.0, -180.0);
}

void CheckGood(double latIn, double latCheck)
{
    Check(latIn, latCheck, -180.0, -180.0);
    Check(latIn, latCheck, -179.99999995, -180.0);
    Check(latIn, latCheck, (-179.99999995).Inc(), -179.9999999);
    Check(latIn, latCheck, (-0.00000005).Dec().Dec(), -0.0000001);
    Check(latIn, latCheck, (-0.00000005).Dec(), 0.0);
    Check(latIn, latCheck, -0.0, 0.0);
    Check(latIn, latCheck, 0.0, 0.0);
    Check(latIn, latCheck, (0.00000005).Inc(), 0.0);
    Check(latIn, latCheck, (0.00000005).Inc().Inc(), 0.0000001);
    Check(latIn, latCheck, (179.99999995).Dec(), 179.9999999);
    Check(latIn, latCheck, 179.99999995, -180.0);
    Check(latIn, latCheck, 180.0, -180.0);
}

void CheckSane(double latIn, double latCheck)
{
    Check(latIn, latCheck, -180.0, -180.0);
    Check(latIn, latCheck, -179.9999999, -180.0);
    Check(latIn, latCheck, (-179.9999999).Inc(), -179.9999998);
    Check(latIn, latCheck, (-0.0000001).Dec().Dec(), -0.0000002);
    Check(latIn, latCheck, (-0.0000001).Dec(), 0.0);
    Check(latIn, latCheck, -0.0, 0.0);
    Check(latIn, latCheck, 0.0, 0.0);
    Check(latIn, latCheck, (0.0000001).Inc(), 0.0);
    Check(latIn, latCheck, (0.0000001).Inc().Inc(), 0.0000002);
    Check(latIn, latCheck, (179.9999999).Dec(), 179.9999998);
    Check(latIn, latCheck, 179.9999999, -180.0);
    Check(latIn, latCheck, 180.0, -180.0);
}

void CheckRough(double latIn, double latCheck)
{
    Check(latIn, latCheck, -180.0, -180.0);
    Check(latIn, latCheck, -179.9999995, -180.0);
    Check(latIn, latCheck, (-179.9999995).Inc(), -179.999999);
    Check(latIn, latCheck, (-0.0000005).Dec(), -0.000001);
    Check(latIn, latCheck, -0.0000005, 0.0);
    Check(latIn, latCheck, -0.0, 0.0);
    Check(latIn, latCheck, 0.0, 0.0);
    Check(latIn, latCheck, 0.0000005, 0.0);
    Check(latIn, latCheck, (0.0000005).Inc(), 0.000001);
    Check(latIn, latCheck, (179.9999995).Dec(), 179.999999);
    Check(latIn, latCheck, 179.9999995, -180.0);
    Check(latIn, latCheck, 180.0, -180.0);
}

void Check(double latIn, double latCheck, double lngIn, double lngCheck)
{
    var latLng = new LatLng64(latIn, lngIn);
    CheckBase(latLng, latCheck, lngCheck);
}

void CheckBase(LatLng64 latLng, double latCheck, double lngCheck)
{
    var (latOut, lngOut) = latLng.GetCoordinates();

    if (latOut != latCheck)
        throw new InvalidOperationException();

    if (lngOut != lngCheck)
        throw new InvalidOperationException();

    CheckNonNegativeZero(latOut);
    CheckNonNegativeZero(lngOut);
}

void CheckNonNegativeZero(double value)
{
    if (value == 0 && 1 / value != double.PositiveInfinity)
        throw new InvalidOperationException();
}

static class Extensions
{
    public static double Inc(this double value) => double.BitIncrement(value);

    public static double Dec(this double value) => double.BitDecrement(value);
}
