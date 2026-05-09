namespace Chubrik.LatLng64;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

/// <summary>
/// Represents a geographic coordinate <c>(latitude, longitude)</c> packed into 64 bits
/// with ±2.8 mm precision in populated latitudes and ±5.6 mm in polar zones.
/// The <see langword="default"/> value is intentionally invalid: <see cref="GetCoordinates"/>
/// throws on it, surfacing uninitialized state instead of silently mapping to a real point.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct LatLng64 :
    IEquatable<LatLng64>, IComparable, IComparable<LatLng64>, IFormattable
#if NET7_0_OR_GREATER
    , ISpanFormattable, IParsable<LatLng64>, ISpanParsable<LatLng64>
#endif
#if NET8_0_OR_GREATER
    , IUtf8SpanFormattable, IUtf8SpanParsable<LatLng64>
#endif
{
    #region Private Constants

    // Approximate boundaries of geographical zones. The AURORA and CENTRAL zones are considered
    // inhabited, whereas the others are not. Each zone applies its own coordinate rounding rules,
    // based on population density and the characteristics of its spherical geometry.
    private const int NORTHERN_TOP = 90;
    private const int NORTHERN_BOTTOM = 85;
    private const int ARCTIC_BOTTOM = 72;
    private const int AURORA_BOTTOM = 60;
    private const int CENTRAL_BOTTOM = -56;
    private const int INTERIM_BOTTOM = -60;
    private const int ANTARCTIC_BOTTOM = -85;
    private const int SOUTHERN_BOTTOM = -90;

    // Number of discrete values per degree for each precision level.
    private const double EXACT_MUL = 20_000_000;
    private const double GOOD_MUL = 10_000_000;
    private const double SANE_MUL = 5_000_000;
    private const double ROUGH_MUL = 1_000_000;

    // Longitude zero-centering offset in encoded units (180 × multiplier), by precision level.
    private const ulong EXACT_MUL_180 = 180 * (ulong)EXACT_MUL;
    private const ulong GOOD_MUL_180 = 180 * (ulong)GOOD_MUL;
    private const ulong SANE_MUL_180 = 180 * (ulong)SANE_MUL;
    private const ulong ROUGH_MUL_180 = 180 * (ulong)ROUGH_MUL;

    // Latitude-step stride in encoded units (360 × multiplier), by precision level.
    private const long EXACT_MUL_360 = 360 * (long)EXACT_MUL;
    private const long GOOD_MUL_360 = 360 * (long)GOOD_MUL;
    private const long SANE_MUL_360 = 360 * (long)SANE_MUL;
    private const long ROUGH_MUL_360 = 360 * (long)ROUGH_MUL;

    // Maximum error in degrees for each precision level.
    private const double EXACT_ERROR = 0.5 / EXACT_MUL; // 0.000000025
    private const double GOOD_ERROR = 0.5 / GOOD_MUL;   // 0.00000005
    private const double SANE_ERROR = 0.5 / SANE_MUL;   // 0.0000001
    private const double ROUGH_ERROR = 0.5 / ROUGH_MUL; // 0.0000005

    // Threshold below which double-precision rounding noise is treated as zero.
    private const double DOUBLE_BIT_ERROR = 1e-14; // 0.00000000000001

    // Maximum input longitude before wrapping to -180 (avoids encoding +180° as a separate bin),
    // by precision level.
    private const double EXACT_MAX_LONGITUDE = 180 - EXACT_ERROR - 2 * DOUBLE_BIT_ERROR; // 179.99999997499998
    private const double GOOD_MAX_LONGITUDE = 180 - GOOD_ERROR - 2 * DOUBLE_BIT_ERROR;   // 179.99999994999996
    private const double SANE_MAX_LONGITUDE = 180 - SANE_ERROR - 2 * DOUBLE_BIT_ERROR;   // 179.99999989999998
    private const double ROUGH_MAX_LONGITUDE = 180 - ROUGH_ERROR - 2 * DOUBLE_BIT_ERROR; // 179.99999949999997

    // Total value range size within the internal _data field for each geographical zone.
    private const ulong NORTHERN_SIZE = ((NORTHERN_TOP - NORTHERN_BOTTOM) * (ulong)GOOD_MUL + 1) * ROUGH_MUL_360;
    private const ulong ARCTIC_SIZE = (NORTHERN_BOTTOM - ARCTIC_BOTTOM) * (ulong)GOOD_MUL * SANE_MUL_360;
    private const ulong AURORA_SIZE = (ARCTIC_BOTTOM - AURORA_BOTTOM) * (ulong)EXACT_MUL * GOOD_MUL_360;
    private const ulong CENTRAL_SIZE = ((AURORA_BOTTOM - CENTRAL_BOTTOM) * (ulong)EXACT_MUL - 1) * EXACT_MUL_360;
    private const ulong INTERIM_SIZE = (CENTRAL_BOTTOM - INTERIM_BOTTOM) * (ulong)GOOD_MUL * GOOD_MUL_360;
    private const ulong ANTARCTIC_SIZE = (INTERIM_BOTTOM - ANTARCTIC_BOTTOM) * (ulong)GOOD_MUL * SANE_MUL_360;
    private const ulong SOUTHERN_SIZE = ((ANTARCTIC_BOTTOM - SOUTHERN_BOTTOM) * (ulong)GOOD_MUL + 1) * ROUGH_MUL_360;

    // Boundary values of the internal _data field:
    // overall maximum, then the minimum (offset) of each geographical zone.
    private const ulong NORTHERN_MAX_DATA = NORTHERN_MIN_DATA + NORTHERN_SIZE - 1; //  18 432 000 000 359 999 999   0x_FFCB_9E57_E975_29FF
    private const ulong NORTHERN_MIN_DATA = ARCTIC_MIN_DATA + ARCTIC_SIZE;         //  18 414 000 000 000 000 000   0x_FF8B_AB70_3E0B_0000
    private const ulong ARCTIC_MIN_DATA = AURORA_MIN_DATA + AURORA_SIZE;           //  18 180 000 000 000 000 000   0x_FC4C_55AD_A09A_0000
    private const ulong AURORA_MIN_DATA = CENTRAL_MIN_DATA + CENTRAL_SIZE;         //  17 316 000 000 000 000 000   0x_F04E_CA41_82AA_0000
    private const ulong CENTRAL_MIN_DATA = INTERIM_MIN_DATA + INTERIM_SIZE;        //     612 000 007 200 000 000   0x_087E_42C3_97B1_4800
    private const ulong INTERIM_MIN_DATA = ANTARCTIC_MIN_DATA + ANTARCTIC_SIZE;    //     468 000 007 200 000 000   0x_067E_AB86_E809_4800
    private const ulong ANTARCTIC_MIN_DATA = SOUTHERN_MIN_DATA + SOUTHERN_SIZE;    //      18 000 007 200 000 000   0x_003F_F2E9_431C_4800
    private const ulong SOUTHERN_MIN_DATA = 6_840_000_000;                         //               6 840 000 000   0x_0000_0001_97B2_1E00

    // Latitude threshold separating adjacent zones, used for zone dispatch in encoding.
    private const double NORTHERN_BOTTOM_ENCODE = NORTHERN_BOTTOM - GOOD_ERROR;                   //  84.99999995
    private const double ARCTIC_BOTTOM_ENCODE = ARCTIC_BOTTOM - EXACT_ERROR;                      //  71.999999975
    private const double AURORA_BOTTOM_ENCODE = AURORA_BOTTOM - EXACT_ERROR - DOUBLE_BIT_ERROR;   //  59.999999974999994
    private const double CENTRAL_BOTTOM_ENCODE = CENTRAL_BOTTOM + EXACT_ERROR + DOUBLE_BIT_ERROR; // -55.999999974999994
    private const double INTERIM_BOTTOM_ENCODE = INTERIM_BOTTOM + GOOD_ERROR + DOUBLE_BIT_ERROR;  // -59.999999949999996
    private const double ANTARCTIC_BOTTOM_ENCODE = ANTARCTIC_BOTTOM + GOOD_ERROR;                 // -84.99999995

    // Shift (offset) used when calculating the internal _data field for each zone.
    private const ulong NORTHERN_SHIFT_ENCODE = NORTHERN_MIN_DATA - NORTHERN_BOTTOM * (ulong)GOOD_MUL * ROUGH_MUL_360 + ROUGH_MUL_180;
    private const ulong ARCTIC_SHIFT_ENCODE = ARCTIC_MIN_DATA - ARCTIC_BOTTOM * (ulong)GOOD_MUL * SANE_MUL_360 + SANE_MUL_180;
    private const ulong AURORA_SHIFT_ENCODE = AURORA_MIN_DATA - AURORA_BOTTOM * (ulong)EXACT_MUL * GOOD_MUL_360 + GOOD_MUL_180;
    private const ulong CENTRAL_SHIFT_ENCODE = CENTRAL_MIN_DATA + (-CENTRAL_BOTTOM * (ulong)EXACT_MUL - 1) * EXACT_MUL_360 + EXACT_MUL_180;
    private const ulong INTERIM_SHIFT_ENCODE = INTERIM_MIN_DATA + (-INTERIM_BOTTOM * (ulong)GOOD_MUL - 1) * GOOD_MUL_360 + GOOD_MUL_180;
    private const ulong ANTARCTIC_SHIFT_ENCODE = ANTARCTIC_MIN_DATA + (-ANTARCTIC_BOTTOM * (ulong)GOOD_MUL - 1) * SANE_MUL_360 + SANE_MUL_180;
    private const ulong SOUTHERN_SHIFT_ENCODE = SOUTHERN_MIN_DATA + (-SOUTHERN_BOTTOM) * (ulong)GOOD_MUL * ROUGH_MUL_360 + ROUGH_MUL_180;

    // Shift (offset) used during coordinate recovery for each zone.
    private const ulong NORTHERN_SHIFT_DECODE = NORTHERN_MIN_DATA;
    private const ulong ARCTIC_SHIFT_DECODE = ARCTIC_MIN_DATA;
    private const ulong AURORA_SHIFT_DECODE = AURORA_MIN_DATA;
    private const ulong CENTRAL_SHIFT_DECODE = CENTRAL_MIN_DATA - EXACT_MUL_360;
    private const ulong INTERIM_SHIFT_DECODE = INTERIM_MIN_DATA - GOOD_MUL_360;
    private const ulong ANTARCTIC_SHIFT_DECODE = ANTARCTIC_MIN_DATA - SANE_MUL_360;
    private const ulong SOUTHERN_SHIFT_DECODE = SOUTHERN_MIN_DATA;

    // Shift (offset) used during latitude reconstruction for each zone.
    private const ulong NORTHERN_ADD_DECODE = (ulong)(NORTHERN_BOTTOM * GOOD_MUL);
    private const ulong ARCTIC_ADD_DECODE = (ulong)(ARCTIC_BOTTOM * GOOD_MUL);
    private const ulong AURORA_ADD_DECODE = (ulong)(AURORA_BOTTOM * EXACT_MUL);
    private const ulong CENTRAL_SUB_DECODE = (ulong)(-CENTRAL_BOTTOM * EXACT_MUL);
    private const ulong INTERIM_SUB_DECODE = (ulong)(-INTERIM_BOTTOM * GOOD_MUL);
    private const ulong ANTARCTIC_SUB_DECODE = (ulong)(-ANTARCTIC_BOTTOM * GOOD_MUL);
    private const ulong SOUTHERN_SUB_DECODE = (ulong)(-SOUTHERN_BOTTOM * GOOD_MUL);

    // Error messages for out-of-range inputs.
    private const string LATITUDE_RANGE_MESSAGE = "Latitude must be between -90 and +90 inclusive.";
    private const string LONGITUDE_RANGE_MESSAGE = "Longitude must be between -180 and +180 inclusive.";
    private const string DATA_RANGE_MESSAGE = "Data is outside the valid encoding range.";

    #endregion

    #region Construction, Encoding

    private readonly ulong _data;

    /// <summary>
    /// Gets the raw 64-bit encoding. Stable across processes; suitable for database persistence.
    /// Reconstruct via <see cref="FromData(ulong)"/>.
    /// </summary>
    public ulong Data => _data;

    // Trusted: caller guarantees the value is a valid encoding. Public entry points (FromData)
    // validate before invoking; Encode produces only in-range values.
    private LatLng64(ulong data)
    {
        _data = data;
    }

    /// <summary>Initializes a new instance of the <see cref="LatLng64"/> struct from latitude
    /// and longitude in degrees.</summary>
    /// <param name="latitude">Degrees in [-90, +90] inclusive.</param>
    /// <param name="longitude">Degrees in [-180, +180] inclusive;
    /// <c>-180</c> and <c>+180</c> are treated as the same meridian.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A coordinate is outside the allowed range, NaN, or infinite.</exception>
    public LatLng64(double latitude, double longitude)
    {
        if (!IsValidLatitude(latitude))
            throw CreateOutOfRangeException(nameof(latitude), latitude, LATITUDE_RANGE_MESSAGE);

        if (!IsValidLongitude(longitude))
            throw CreateOutOfRangeException(nameof(longitude), longitude, LONGITUDE_RANGE_MESSAGE);

        _data = EncodeCore(latitude, longitude);
    }

    /// <summary>Deconstructs this instance into its latitude and longitude components.</summary>
    /// <param name="latitude">The decoded latitude in degrees.</param>
    /// <param name="longitude">The decoded longitude in degrees.</param>
    /// <exception cref="InvalidOperationException">
    /// The instance is uninitialized (<see langword="default"/>).</exception>
    public void Deconstruct(out double latitude, out double longitude)
    {
        (latitude, longitude) = GetCoordinates();
    }

    // Trusted: caller guarantees latitude in [-90, +90] and longitude in [-180, +180].
    // Public entry points (constructor, Parse, TryParse) validate before invoking.
    private static ulong EncodeCore(double latitude, double longitude)
    {
        unchecked
        {
            if (latitude > CENTRAL_BOTTOM_ENCODE)
            {
                if (latitude < AURORA_BOTTOM_ENCODE)
                {
                    if (longitude > EXACT_MAX_LONGITUDE)
                        longitude = -180;

                    return CENTRAL_SHIFT_ENCODE +
                        Quantize(latitude * EXACT_MUL) * EXACT_MUL_360 +
                        Quantize(longitude * EXACT_MUL);
                }
                else if (latitude < ARCTIC_BOTTOM_ENCODE)
                {
                    if (longitude > GOOD_MAX_LONGITUDE)
                        longitude = -180;

                    return AURORA_SHIFT_ENCODE +
                        Quantize(latitude * EXACT_MUL) * GOOD_MUL_360 +
                        Quantize(longitude * GOOD_MUL);
                }
                else if (latitude < NORTHERN_BOTTOM_ENCODE)
                {
                    if (longitude > SANE_MAX_LONGITUDE)
                        longitude = -180;

                    return ARCTIC_SHIFT_ENCODE +
                        Quantize(latitude * GOOD_MUL) * SANE_MUL_360 +
                        Quantize(longitude * SANE_MUL);
                }
                else
                {
                    if (longitude > ROUGH_MAX_LONGITUDE)
                        longitude = -180;

                    return NORTHERN_SHIFT_ENCODE +
                        Quantize(latitude * GOOD_MUL) * ROUGH_MUL_360 +
                        Quantize(longitude * ROUGH_MUL);
                }
            }
            else if (latitude > INTERIM_BOTTOM_ENCODE)
            {
                if (longitude > GOOD_MAX_LONGITUDE)
                    longitude = -180;

                return INTERIM_SHIFT_ENCODE +
                    Quantize(latitude * GOOD_MUL) * GOOD_MUL_360 +
                    Quantize(longitude * GOOD_MUL);
            }
            else if (latitude > ANTARCTIC_BOTTOM_ENCODE)
            {
                if (longitude > SANE_MAX_LONGITUDE)
                    longitude = -180;

                return ANTARCTIC_SHIFT_ENCODE +
                    Quantize(latitude * GOOD_MUL) * SANE_MUL_360 +
                    Quantize(longitude * SANE_MUL);
            }
            else
            {
                if (longitude > ROUGH_MAX_LONGITUDE)
                    longitude = -180;

                return SOUTHERN_SHIFT_ENCODE +
                    Quantize(latitude * GOOD_MUL) * ROUGH_MUL_360 +
                    Quantize(longitude * ROUGH_MUL);
            }
        }
    }

    /// <summary>Decodes the stored value into latitude and longitude in degrees.</summary>
    /// <returns>A tuple containing the decoded latitude and longitude.</returns>
    /// <exception cref="InvalidOperationException">
    /// The instance is uninitialized (<see langword="default"/>).</exception>
    public (double Latitude, double Longitude) GetCoordinates()
    {
        double latitude, longitude;

        unchecked
        {
            if (_data >= CENTRAL_MIN_DATA)
            {
                if (_data < AURORA_MIN_DATA)
                {
                    var (quotient, remainder) = DivRem(_data - CENTRAL_SHIFT_DECODE, EXACT_MUL_360);
                    latitude = (long)(quotient - CENTRAL_SUB_DECODE) / EXACT_MUL;
                    longitude = (long)(remainder - EXACT_MUL_180) / EXACT_MUL;
                }
                else if (_data < ARCTIC_MIN_DATA)
                {
                    var (quotient, remainder) = DivRem(_data - AURORA_SHIFT_DECODE, GOOD_MUL_360);
                    latitude = (quotient + AURORA_ADD_DECODE) / EXACT_MUL;
                    longitude = (long)(remainder - GOOD_MUL_180) / GOOD_MUL;
                }
                else if (_data < NORTHERN_MIN_DATA)
                {
                    var (quotient, remainder) = DivRem(_data - ARCTIC_SHIFT_DECODE, SANE_MUL_360);
                    latitude = (quotient + ARCTIC_ADD_DECODE) / GOOD_MUL;
                    longitude = (long)(remainder - SANE_MUL_180) / SANE_MUL;
                }
                else if (_data <= NORTHERN_MAX_DATA)
                {
                    var (quotient, remainder) = DivRem(_data - NORTHERN_SHIFT_DECODE, ROUGH_MUL_360);
                    latitude = (quotient + NORTHERN_ADD_DECODE) / GOOD_MUL;
                    longitude = (long)(remainder - ROUGH_MUL_180) / ROUGH_MUL;
                }
                else
                    throw CreateInvalidDataException();
            }
            else if (_data >= INTERIM_MIN_DATA)
            {
                var (quotient, remainder) = DivRem(_data - INTERIM_SHIFT_DECODE, GOOD_MUL_360);
                latitude = (long)(quotient - INTERIM_SUB_DECODE) / GOOD_MUL;
                longitude = (long)(remainder - GOOD_MUL_180) / GOOD_MUL;
            }
            else if (_data >= ANTARCTIC_MIN_DATA)
            {
                var (quotient, remainder) = DivRem(_data - ANTARCTIC_SHIFT_DECODE, SANE_MUL_360);
                latitude = (long)(quotient - ANTARCTIC_SUB_DECODE) / GOOD_MUL;
                longitude = (long)(remainder - SANE_MUL_180) / SANE_MUL;
            }
            else if (_data >= SOUTHERN_MIN_DATA)
            {
                var (quotient, remainder) = DivRem(_data - SOUTHERN_SHIFT_DECODE, ROUGH_MUL_360);
                latitude = (long)(quotient - SOUTHERN_SUB_DECODE) / GOOD_MUL;
                longitude = (long)(remainder - ROUGH_MUL_180) / ROUGH_MUL;
            }
            else
                throw CreateInvalidDataException();
        }

        return (latitude, longitude);
    }

    /// <summary>Reconstructs a <see cref="LatLng64"/> from a raw 64-bit encoding previously
    /// obtained from <see cref="Data"/>.</summary>
    /// <param name="data">A raw 64-bit encoding.</param>
    /// <returns>A <see cref="LatLng64"/> equivalent to the encoding in
    /// <paramref name="data"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="data"/> is outside the valid encoding range.</exception>
    public static LatLng64 FromData(ulong data)
    {
        if (data < SOUTHERN_MIN_DATA || data > NORTHERN_MAX_DATA)
            throw CreateOutOfRangeException(nameof(data), data, DATA_RANGE_MESSAGE);

        return new LatLng64(data);
    }

    // NaN-safe range checks shared by the (double, double) constructor and the parse path.
    private static bool IsValidLatitude(double value) => value >= -90 && value <= 90;
    private static bool IsValidLongitude(double value) => value >= -180 && value <= 180;

    // Snaps a scaled coordinate onto the integer grid; AwayFromZero keeps ties symmetric.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Quantize(double value)
    {
        unchecked
        {
            return (ulong)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong Quotient, ulong Remainder) DivRem(ulong dividend, ulong divisor)
    {
#if NET6_0_OR_GREATER
        return Math.DivRem(dividend, divisor);
#else
        var quotient = dividend / divisor;
        return (quotient, dividend - quotient * divisor);
#endif
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ArgumentOutOfRangeException CreateOutOfRangeException(
        string paramName, object value, string message)
    {
        return new ArgumentOutOfRangeException(paramName, value, message);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static InvalidOperationException CreateInvalidDataException()
    {
        return new InvalidOperationException(
            "The LatLng64 is uninitialized (default) or contains invalid data.");
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay =>
        _data == 0 ? "<default>" : ToString(format: null, CultureInfo.InvariantCulture);

    #endregion

    #region IEquatable, IComparable

    /// <summary>Returns a value indicating whether this instance and another <see cref="LatLng64"/>
    /// represent the same encoded coordinate.</summary>
    /// <param name="other">A <see cref="LatLng64"/> to compare with this instance.</param>
    /// <returns><see langword="true"/> if <paramref name="other"/> equals this instance;
    /// otherwise, <see langword="false"/>.</returns>
    /// <remarks><see langword="default"/> equals only itself; this method does not throw,
    /// per the <see cref="IEquatable{T}"/> contract.</remarks>
    public bool Equals(LatLng64 other) => _data == other._data;

    /// <summary>Returns a value indicating whether this instance and a specified object
    /// represent the same encoded coordinate.</summary>
    /// <param name="obj">An object to compare with this instance.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="LatLng64"/>
    /// and equals this instance; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is LatLng64 latLng && Equals(latLng);

    /// <inheritdoc/>
    public override int GetHashCode() => _data.GetHashCode();

    /// <summary>Returns a value indicating whether two instances represent the same
    /// encoded coordinate.</summary>
    public static bool operator ==(LatLng64 left, LatLng64 right) => left.Equals(right);

    /// <summary>Returns a value indicating whether two instances represent different
    /// encoded coordinates.</summary>
    public static bool operator !=(LatLng64 left, LatLng64 right) => !(left == right);

    /// <summary>Compares this instance with another <see cref="LatLng64"/> by raw
    /// <see cref="Data"/>. The ordering is deterministic and suitable for sorting,
    /// deduplication, and database indexing, but is NOT a spatial index — geographically
    /// close points may be far apart in this order.</summary>
    /// <param name="other">A <see cref="LatLng64"/> to compare with this instance.</param>
    /// <returns>A signed integer that indicates the relative order of this instance
    /// and <paramref name="other"/>.</returns>
    /// <remarks><see langword="default"/> orders below all valid values; this method does not
    /// throw, per the <see cref="IComparable{T}"/> contract.</remarks>
    public int CompareTo(LatLng64 other) => _data.CompareTo(other._data);

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">
    /// <paramref name="obj"/> is neither <see langword="null"/> nor a <see cref="LatLng64"/>.
    /// </exception>
    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;

        if (obj is LatLng64 other)
            return CompareTo(other);

        throw new ArgumentException($"Object must be of type {nameof(LatLng64)}.", nameof(obj));
    }

    #endregion

    #region IFormattable, ISpanFormattable

    /// <inheritdoc/>
    public override string ToString()
    {
        return ToString(format: null, formatProvider: null);
    }

    /// <summary>Converts the value of this instance to its equivalent string representation
    /// using the specified format.</summary>
    /// <param name="format">A standard or custom numeric format string applied to each axis.
    /// Empty or <see langword="null"/> defaults to <c>"0.########"</c> —
    /// full precision, no trailing zeros, no exponential form.</param>
    /// <returns>The string representation of the coordinates as specified
    /// by <paramref name="format"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The instance is uninitialized (<see langword="default"/>).</exception>
    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
    {
        return ToString(format, formatProvider: null);
    }

    /// <summary>Converts the value of this instance to its equivalent string representation
    /// using the specified culture-specific format information.</summary>
    /// <param name="formatProvider">A culture-specific format provider;
    /// <see langword="null"/> defaults to <see cref="CultureInfo.CurrentCulture"/>.</param>
    /// <returns>The string representation of the coordinates as specified
    /// by <paramref name="formatProvider"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The instance is uninitialized (<see langword="default"/>).</exception>
    public string ToString(IFormatProvider? formatProvider)
    {
        return ToString(format: null, formatProvider);
    }

    /// <summary>Converts the value of this instance to its equivalent string representation
    /// as <c>"latitude, longitude"</c> (or <c>"latitude; longitude"</c> for cultures whose
    /// decimal separator is a comma), using the specified format and culture-specific format
    /// information.</summary>
    /// <param name="format">A standard or custom numeric format string applied to each axis.
    /// Empty or <see langword="null"/> defaults to <c>"0.########"</c> —
    /// full precision, no trailing zeros, no exponential form.</param>
    /// <param name="formatProvider">A culture-specific format provider;
    /// <see langword="null"/> defaults to <see cref="CultureInfo.CurrentCulture"/>.</param>
    /// <returns>The string representation of the coordinates as specified by
    /// <paramref name="format"/> and <paramref name="formatProvider"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The instance is uninitialized (<see langword="default"/>).</exception>
    public string ToString(
        [StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format,
        IFormatProvider? formatProvider)
    {
#if NET7_0_OR_GREATER
        Span<char> buffer = stackalloc char[64];

        if (TryFormat(buffer, out var charsWritten, format.AsSpan(), formatProvider))
            return buffer[..charsWritten].ToString();

        // Long custom format (e.g. "F30") overflows the 64-char stackalloc — fall through.
#endif
        var (latitude, longitude) = GetCoordinates();
        formatProvider ??= CultureInfo.CurrentCulture;
        var formatInfo = NumberFormatInfo.GetInstance(formatProvider);
        var separator = formatInfo.NumberDecimalSeparator == "," ? "; " : ", ";

        if (string.IsNullOrEmpty(format))
            format = "0.########"; // Full precision, no trailing zeros, no exponential form

        return latitude.ToString(format, formatProvider)
             + separator
             + longitude.ToString(format, formatProvider);
    }

#if NET7_0_OR_GREATER

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// The instance is uninitialized (<see langword="default"/>).</exception>
    public bool TryFormat(
        Span<char> destination, out int charsWritten,
        [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null)
    {
        var (latitude, longitude) = GetCoordinates();
        provider ??= CultureInfo.CurrentCulture;
        var formatInfo = NumberFormatInfo.GetInstance(provider);
        var separator = (formatInfo.NumberDecimalSeparator == "," ? "; " : ", ").AsSpan();

        if (format.IsEmpty)
            format = "0.########"; // Full precision, no trailing zeros, no exponential form

        if (!latitude.TryFormat(destination, out var latLen, format, provider) ||
            destination.Length - latLen < separator.Length)
        {
            charsWritten = 0;
            return false;
        }

        separator.CopyTo(destination[latLen..]);
        var offset = latLen + separator.Length;

        if (!longitude.TryFormat(destination[offset..], out var lngLen, format, provider))
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = offset + lngLen;
        return true;
    }

#endif

#if NET8_0_OR_GREATER

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// The instance is uninitialized (<see langword="default"/>).</exception>
    public bool TryFormat(
        Span<byte> utf8Destination, out int bytesWritten,
        [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null)
    {
        var (latitude, longitude) = GetCoordinates();
        provider ??= CultureInfo.CurrentCulture;
        var formatInfo = NumberFormatInfo.GetInstance(provider);
        var separator = formatInfo.NumberDecimalSeparator == "," ? "; "u8 : ", "u8;

        if (format.IsEmpty)
            format = "0.########"; // Full precision, no trailing zeros, no exponential form

        if (!latitude.TryFormat(utf8Destination, out var latLen, format, provider) ||
            utf8Destination.Length - latLen < separator.Length)
        {
            bytesWritten = 0;
            return false;
        }

        separator.CopyTo(utf8Destination[latLen..]);
        var offset = latLen + separator.Length;

        if (!longitude.TryFormat(utf8Destination[offset..], out var lngLen, format, provider))
        {
            bytesWritten = 0;
            return false;
        }

        bytesWritten = offset + lngLen;
        return true;
    }

#endif
    #endregion

    #region IParsable, ISpanParsable

    /// <summary>Converts the string representation of a coordinate pair to its
    /// <see cref="LatLng64"/> equivalent. The input is expected as
    /// <c>"latitude, longitude"</c> (or <c>"latitude; longitude"</c> for cultures whose
    /// decimal separator is a comma), parsed using the current culture.</summary>
    /// <param name="s">A string containing the coordinate pair to convert.</param>
    /// <returns>The <see cref="LatLng64"/> equivalent of <paramref name="s"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="s"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">
    /// <paramref name="s"/> is not in a recognized format.</exception>
    /// <exception cref="OverflowException">
    /// A coordinate is outside the allowed range, NaN, or infinite.</exception>
    public static LatLng64 Parse(string s)
    {
        return Parse(s, provider: null);
    }

    /// <summary>Converts the string representation of a coordinate pair to its
    /// <see cref="LatLng64"/> equivalent, using the specified culture-specific format
    /// information. The input is expected as <c>"latitude, longitude"</c> (or
    /// <c>"latitude; longitude"</c> for cultures whose decimal separator is a comma);
    /// the expected pair separator is derived from <paramref name="provider"/>.</summary>
    /// <param name="s">A string containing the coordinate pair to convert.</param>
    /// <param name="provider">A culture-specific format provider;
    /// <see langword="null"/> defaults to <see cref="CultureInfo.CurrentCulture"/>.</param>
    /// <returns>The <see cref="LatLng64"/> equivalent of <paramref name="s"/> as specified
    /// by <paramref name="provider"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="s"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">
    /// <paramref name="s"/> is not in a recognized format.</exception>
    /// <exception cref="OverflowException">
    /// A coordinate is outside the allowed range, NaN, or infinite.</exception>
    public static LatLng64 Parse(string s, IFormatProvider? provider)
    {
#if NET
        ArgumentNullException.ThrowIfNull(s);
#else
        if (s is null)
            throw new ArgumentNullException(nameof(s));
#endif
        return ParseCore(s, provider);
    }

    /// <summary>Tries to convert the string representation of a coordinate pair to its
    /// <see cref="LatLng64"/> equivalent. A return value indicates whether the conversion
    /// succeeded or failed.</summary>
    /// <param name="s">A string containing the coordinate pair to convert.</param>
    /// <param name="result">When this method returns, contains the <see cref="LatLng64"/>
    /// equivalent of <paramref name="s"/> if the conversion succeeded; otherwise,
    /// <see langword="default"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> was converted successfully;
    /// otherwise, <see langword="false"/>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out LatLng64 result)
    {
        return TryParse(s, provider: null, out result);
    }

    /// <inheritdoc cref="TryParse(string?, out LatLng64)"/>
    /// <param name="s">A string containing the coordinate pair to convert.</param>
    /// <param name="provider">A culture-specific format provider;
    /// <see langword="null"/> defaults to <see cref="CultureInfo.CurrentCulture"/>.</param>
    /// <param name="result">When this method returns, contains the <see cref="LatLng64"/>
    /// equivalent of <paramref name="s"/> if the conversion succeeded; otherwise,
    /// <see langword="default"/>.</param>
    public static bool TryParse(
        [NotNullWhen(true)] string? s, IFormatProvider? provider, out LatLng64 result)
    {
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParseCore(s, provider, out result) == ParseError.None;
    }

#if NET7_0_OR_GREATER

    /// <inheritdoc cref="Parse(string)"
    /// path="/*[not(self::exception[@cref='T:System.ArgumentNullException'])]"/>
    public static LatLng64 Parse(ReadOnlySpan<char> s)
    {
        return ParseCore(s, provider: null);
    }

    /// <inheritdoc cref="Parse(string, IFormatProvider?)"
    /// path="/*[not(self::exception[@cref='T:System.ArgumentNullException'])]"/>
    public static LatLng64 Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return ParseCore(s, provider);
    }

    /// <inheritdoc cref="TryParse(string?, out LatLng64)"/>
    public static bool TryParse(ReadOnlySpan<char> s, out LatLng64 result)
    {
        return TryParse(s, provider: null, out result);
    }

    /// <inheritdoc cref="TryParse(string?, IFormatProvider?, out LatLng64)"/>
    public static bool TryParse(
        ReadOnlySpan<char> s, IFormatProvider? provider, out LatLng64 result)
    {
        return TryParseCore(s, provider, out result) == ParseError.None;
    }

#endif

#if NET8_0_OR_GREATER

    /// <inheritdoc cref="Parse(string)"
    /// path="/*[not(self::exception[@cref='T:System.ArgumentNullException'])]"/>
    public static LatLng64 Parse(ReadOnlySpan<byte> utf8Text)
    {
        return Parse(utf8Text, provider: null);
    }

    /// <inheritdoc cref="Parse(string, IFormatProvider?)"
    /// path="/*[not(self::exception[@cref='T:System.ArgumentNullException'])]"/>
    public static LatLng64 Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
    {
        var error = TryParseCore(utf8Text, provider, out var result);

        if (error == ParseError.None)
            return result;

        throw CreateParseException(error, provider);
    }

    /// <inheritdoc cref="TryParse(string?, out LatLng64)"/>
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, out LatLng64 result)
    {
        return TryParse(utf8Text, provider: null, out result);
    }

    /// <inheritdoc cref="TryParse(string?, IFormatProvider?, out LatLng64)"/>
    public static bool TryParse(
        ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out LatLng64 result)
    {
        return TryParseCore(utf8Text, provider, out result) == ParseError.None;
    }

#endif

    private static LatLng64 ParseCore(
#if NET7_0_OR_GREATER
        ReadOnlySpan<char> s,
#else
        string s,
#endif
        IFormatProvider? provider)
    {
        var error = TryParseCore(s, provider, out var result);

        if (error == ParseError.None)
            return result;

        throw CreateParseException(error, provider);
    }

    private static ParseError TryParseCore(
#if NET7_0_OR_GREATER
        ReadOnlySpan<char> s,
#else
        string s,
#endif
        IFormatProvider? provider, out LatLng64 result)
    {
        result = default;

        if (s.Length == 0)
            return ParseError.BadFormat;

        provider ??= CultureInfo.CurrentCulture;
        var formatInfo = NumberFormatInfo.GetInstance(provider);
        var pairSeparator = formatInfo.NumberDecimalSeparator == "," ? ';' : ',';

        var sepIndex = s.IndexOf(pairSeparator);

        if (sepIndex == -1)
            return ParseError.BadFormat;

        var latSpan = s[..sepIndex];
        var lngSpan = s[(sepIndex + 1)..];

        if (!double.TryParse(latSpan, NumberStyles.Float, provider, out var latitude) ||
            !double.TryParse(lngSpan, NumberStyles.Float, provider, out var longitude))
            return ParseError.BadFormat;

        return EncodeIfInRange(latitude, longitude, out result);
    }

#if NET8_0_OR_GREATER

    private static ParseError TryParseCore(
        ReadOnlySpan<byte> s, IFormatProvider? provider, out LatLng64 result)
    {
        result = default;

        if (s.Length == 0)
            return ParseError.BadFormat;

        provider ??= CultureInfo.CurrentCulture;
        var formatInfo = NumberFormatInfo.GetInstance(provider);
        var pairSeparator = formatInfo.NumberDecimalSeparator == "," ? (byte)';' : (byte)',';

        var sepIndex = s.IndexOf(pairSeparator);

        if (sepIndex == -1)
            return ParseError.BadFormat;

        var latSpan = s[..sepIndex];
        var lngSpan = s[(sepIndex + 1)..];

        if (!double.TryParse(latSpan, NumberStyles.Float, provider, out var latitude) ||
            !double.TryParse(lngSpan, NumberStyles.Float, provider, out var longitude))
            return ParseError.BadFormat;

        return EncodeIfInRange(latitude, longitude, out result);
    }

#endif

    private static ParseError EncodeIfInRange(
        double latitude, double longitude, out LatLng64 result)
    {
        if (!IsValidLatitude(latitude))
        {
            result = default;
            return ParseError.LatitudeOutOfRange;
        }

        if (!IsValidLongitude(longitude))
        {
            result = default;
            return ParseError.LongitudeOutOfRange;
        }

        var data = EncodeCore(latitude, longitude);
        result = new LatLng64(data);
        return ParseError.None;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Exception CreateParseException(ParseError error, IFormatProvider? provider)
    {
        switch (error)
        {
            case ParseError.LatitudeOutOfRange:
                return new OverflowException(LATITUDE_RANGE_MESSAGE);
            case ParseError.LongitudeOutOfRange:
                return new OverflowException(LONGITUDE_RANGE_MESSAGE);
            default:
                var resolved = provider ?? CultureInfo.CurrentCulture;
                var formatInfo = NumberFormatInfo.GetInstance(resolved);
                var expected = formatInfo.NumberDecimalSeparator == ","
                    ? "'latitude; longitude'"
                    : "'latitude, longitude'";
                var cultureName = resolved is CultureInfo c
                    ? (c.Name.Length > 0 ? c.Name : "Invariant")
                    : resolved.GetType().Name;
                var cultureSource = provider is null ? "current" : "provided";
                return new FormatException(
                    "The input string was not in a correct format. " +
                    "Expected: " + expected + " for the " + cultureSource +
                    " culture (" + cultureName + ").");
        }
    }

    private enum ParseError : byte
    {
        None,
        BadFormat,
        LatitudeOutOfRange,
        LongitudeOutOfRange,
    }

    #endregion
}
