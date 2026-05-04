namespace Chubrik.LatLng64.Tests;

internal static class DoubleBitExtensions
{
#if NET
    public static double Inc(this double value) => double.BitIncrement(value);

    public static double Dec(this double value) => double.BitDecrement(value);

    public static bool IsNeg(this double value) => double.IsNegative(value);
#else
    public static double Inc(this double value)
    {
        var bits = System.BitConverter.DoubleToInt64Bits(value);
        if (bits == long.MinValue) return double.Epsilon;
        return System.BitConverter.Int64BitsToDouble(bits + (value.IsNeg() ? -1 : 1));
    }

    public static double Dec(this double value)
    {
        var bits = System.BitConverter.DoubleToInt64Bits(value);
        if (bits == 0L) return -double.Epsilon;
        return System.BitConverter.Int64BitsToDouble(bits + (value.IsNeg() ? 1 : -1));
    }

    public static bool IsNeg(this double value) => System.BitConverter.DoubleToInt64Bits(value) < 0;
#endif
}
