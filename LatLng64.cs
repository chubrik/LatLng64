namespace Chubrik.LatLng64;

public readonly struct LatLng64 : IEquatable<LatLng64>
{
    #region Constants

    private const int NORTHERN_TOP = 90;
    private const int NORTHERN_BOTTOM = 85;
    private const int ARCTIC_BOTTOM = 72;
    private const int AURORA_BOTTOM = 60;
    private const int CENTRAL_BOTTOM = -56;
    private const int INTERIM_BOTTOM = -60;
    private const int ANTARCTIC_BOTTOM = -85;
    private const int SOUTHERN_BOTTOM = -90;

    private const double EXACT_MUL = 20_000_000;
    private const double GOOD_MUL = 10_000_000;
    private const double SANE_MUL = 5_000_000;
    private const double ROUGH_MUL = 1_000_000;

    private const ulong EXACT_MUL_180 = 180 * (ulong)EXACT_MUL;
    private const ulong GOOD_MUL_180 = 180 * (ulong)GOOD_MUL;
    private const ulong SANE_MUL_180 = 180 * (ulong)SANE_MUL;
    private const ulong ROUGH_MUL_180 = 180 * (ulong)ROUGH_MUL;

    private const long EXACT_MUL_360 = 360 * (long)EXACT_MUL;
    private const long GOOD_MUL_360 = 360 * (long)GOOD_MUL;
    private const long SANE_MUL_360 = 360 * (long)SANE_MUL;
    private const long ROUGH_MUL_360 = 360 * (long)ROUGH_MUL;

    private const double EXACT_ERROR = 0.5 / EXACT_MUL;
    private const double GOOD_ERROR = 0.5 / GOOD_MUL;
    private const double SANE_ERROR = 0.5 / SANE_MUL;
    private const double ROUGH_ERROR = 0.5 / ROUGH_MUL;

    private const double EXACT_MAX_LONGITUDE = 180 - EXACT_ERROR;
    private const double GOOD_MAX_LONGITUDE = 180 - GOOD_ERROR;
    private const double SANE_MAX_LONGITUDE = 180 - SANE_ERROR;
    private const double ROUGH_MAX_LONGITUDE = 180 - ROUGH_ERROR;

    private const ulong NORTHERN_SIZE = ((NORTHERN_TOP - NORTHERN_BOTTOM) * (ulong)GOOD_MUL + 1) * ROUGH_MUL_360;
    private const ulong ARCTIC_SIZE = (NORTHERN_BOTTOM - ARCTIC_BOTTOM) * (ulong)GOOD_MUL * SANE_MUL_360;
    private const ulong AURORA_SIZE = (ARCTIC_BOTTOM - AURORA_BOTTOM) * (ulong)EXACT_MUL * GOOD_MUL_360;
    private const ulong CENTRAL_SIZE = ((AURORA_BOTTOM - CENTRAL_BOTTOM) * (ulong)EXACT_MUL - 1) * EXACT_MUL_360;
    private const ulong INTERIM_SIZE = (CENTRAL_BOTTOM - INTERIM_BOTTOM) * (ulong)GOOD_MUL * GOOD_MUL_360;
    private const ulong ANTARCTIC_SIZE = (INTERIM_BOTTOM - ANTARCTIC_BOTTOM) * (ulong)GOOD_MUL * SANE_MUL_360;
    private const ulong SOUTHERN_SIZE = ((ANTARCTIC_BOTTOM - SOUTHERN_BOTTOM) * (ulong)GOOD_MUL + 1) * ROUGH_MUL_360;

    private const ulong MAX_VALUE = NORTHERN_MIN_VALUE + NORTHERN_SIZE - 1;
    private const ulong NORTHERN_MIN_VALUE = ARCTIC_MIN_VALUE + ARCTIC_SIZE;
    private const ulong ARCTIC_MIN_VALUE = AURORA_MIN_VALUE + AURORA_SIZE;
    private const ulong AURORA_MIN_VALUE = CENTRAL_MIN_VALUE + CENTRAL_SIZE;
    private const ulong CENTRAL_MIN_VALUE = INTERIM_MIN_VALUE + INTERIM_SIZE;
    private const ulong INTERIM_MIN_VALUE = ANTARCTIC_MIN_VALUE + ANTARCTIC_SIZE;
    private const ulong ANTARCTIC_MIN_VALUE = SOUTHERN_MIN_VALUE + SOUTHERN_SIZE;
    private const ulong SOUTHERN_MIN_VALUE = 0;

    private const double DOUBLE_BIT_ERROR = 1e-14;
    private const double NORTHERN_BOTTOM_ENCODE = NORTHERN_BOTTOM - GOOD_ERROR;
    private const double ARCTIC_BOTTOM_ENCODE = ARCTIC_BOTTOM - EXACT_ERROR - DOUBLE_BIT_ERROR;
    private const double AURORA_BOTTOM_ENCODE = AURORA_BOTTOM - EXACT_ERROR - DOUBLE_BIT_ERROR;
    private const double CENTRAL_BOTTOM_ENCODE = CENTRAL_BOTTOM + EXACT_ERROR + DOUBLE_BIT_ERROR;
    private const double INTERIM_BOTTOM_ENCODE = INTERIM_BOTTOM + GOOD_ERROR + DOUBLE_BIT_ERROR;
    private const double ANTARCTIC_BOTTOM_ENCODE = ANTARCTIC_BOTTOM + GOOD_ERROR;

    private const ulong NORTHERN_SHIFT_ENCODE = NORTHERN_MIN_VALUE - NORTHERN_BOTTOM * (ulong)GOOD_MUL * ROUGH_MUL_360 + ROUGH_MUL_180;
    private const ulong ARCTIC_SHIFT_ENCODE = ARCTIC_MIN_VALUE - ARCTIC_BOTTOM * (ulong)GOOD_MUL * SANE_MUL_360 + SANE_MUL_180;
    private const ulong AURORA_SHIFT_ENCODE = AURORA_MIN_VALUE - AURORA_BOTTOM * (ulong)EXACT_MUL * GOOD_MUL_360 + GOOD_MUL_180;
    private const ulong CENTRAL_SHIFT_ENCODE = CENTRAL_MIN_VALUE + (-CENTRAL_BOTTOM * (ulong)EXACT_MUL - 1) * EXACT_MUL_360 + EXACT_MUL_180;
    private const ulong INTERIM_SHIFT_ENCODE = INTERIM_MIN_VALUE + (-INTERIM_BOTTOM * (ulong)GOOD_MUL - 1) * GOOD_MUL_360 + GOOD_MUL_180;
    private const ulong ANTARCTIC_SHIFT_ENCODE = ANTARCTIC_MIN_VALUE + (-ANTARCTIC_BOTTOM * (ulong)GOOD_MUL - 1) * SANE_MUL_360 + SANE_MUL_180;
    private const ulong SOUTHERN_SHIFT_ENCODE = SOUTHERN_MIN_VALUE + (-SOUTHERN_BOTTOM) * (ulong)GOOD_MUL * ROUGH_MUL_360 + ROUGH_MUL_180;

    private const ulong NORTHERN_SHIFT_DECODE = NORTHERN_MIN_VALUE;
    private const ulong ARCTIC_SHIFT_DECODE = ARCTIC_MIN_VALUE;
    private const ulong AURORA_SHIFT_DECODE = AURORA_MIN_VALUE;
    private const ulong CENTRAL_SHIFT_DECODE = CENTRAL_MIN_VALUE - EXACT_MUL_360;
    private const ulong INTERIM_SHIFT_DECODE = INTERIM_MIN_VALUE - GOOD_MUL_360;
    private const ulong ANTARCTIC_SHIFT_DECODE = ANTARCTIC_MIN_VALUE - SANE_MUL_360;

    private const ulong NORTHERN_ADD_DECODE = (ulong)(NORTHERN_BOTTOM * GOOD_MUL);
    private const ulong ARCTIC_ADD_DECODE = (ulong)(ARCTIC_BOTTOM * GOOD_MUL);
    private const ulong AURORA_ADD_DECODE = (ulong)(AURORA_BOTTOM * EXACT_MUL);
    private const ulong CENTRAL_SUB_DECODE = (ulong)(-CENTRAL_BOTTOM * EXACT_MUL);
    private const ulong INTERIM_SUB_DECODE = (ulong)(-INTERIM_BOTTOM * GOOD_MUL);
    private const ulong ANTARCTIC_SUB_DECODE = (ulong)(-ANTARCTIC_BOTTOM * GOOD_MUL);
    private const ulong SOUTHERN_SUB_DECODE = (ulong)(-SOUTHERN_BOTTOM * GOOD_MUL);

    #endregion

    private readonly ulong _data;

    public LatLng64()
    {
        _data = CENTRAL_SHIFT_ENCODE;
    }

    private LatLng64(ulong data)
    {
        if (data > MAX_VALUE)
            throw new ArgumentOutOfRangeException(nameof(data));

        _data = data;
    }

    public LatLng64(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude),
                $"Requires a number between -90 and +90 inclusive, but the actual value is: {latitude}");

        if (double.IsNaN(latitude) || longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude),
                $"Requires a number between -180 and +180 inclusive, but the actual value is: {longitude}");

        if (latitude > CENTRAL_BOTTOM_ENCODE)
        {
            if (latitude < AURORA_BOTTOM_ENCODE)
            {
                if (longitude >= EXACT_MAX_LONGITUDE)
                    longitude = -180;

                _data = CENTRAL_SHIFT_ENCODE + (ulong)(
                    (long)Math.Round(latitude * EXACT_MUL) * EXACT_MUL_360 + (long)Math.Round(longitude * EXACT_MUL));
            }
            else if (latitude < ARCTIC_BOTTOM_ENCODE)
            {
                if (longitude >= GOOD_MAX_LONGITUDE)
                    longitude = -180;

                _data = AURORA_SHIFT_ENCODE + (ulong)(
                    (long)Math.Round(latitude * EXACT_MUL) * GOOD_MUL_360 + (long)Math.Round(longitude * GOOD_MUL));
            }
            else if (latitude < NORTHERN_BOTTOM_ENCODE)
            {
                if (longitude >= SANE_MAX_LONGITUDE)
                    longitude = -180;

                _data = ARCTIC_SHIFT_ENCODE + (ulong)(
                    (long)Math.Round(latitude * GOOD_MUL) * SANE_MUL_360 + (long)Math.Round(longitude * SANE_MUL));
            }
            else
            {
                if (longitude >= ROUGH_MAX_LONGITUDE)
                    longitude = -180;

                _data = NORTHERN_SHIFT_ENCODE + (ulong)(
                    (long)Math.Round(latitude * GOOD_MUL) * ROUGH_MUL_360 + (long)Math.Round(longitude * ROUGH_MUL));
            }
        }
        else if (latitude > INTERIM_BOTTOM_ENCODE)
        {
            if (longitude >= GOOD_MAX_LONGITUDE)
                longitude = -180;

            _data = INTERIM_SHIFT_ENCODE + (ulong)(
                (long)Math.Round(latitude * GOOD_MUL) * GOOD_MUL_360 + (long)Math.Round(longitude * GOOD_MUL));
        }
        else if (latitude > ANTARCTIC_BOTTOM_ENCODE)
        {
            if (longitude >= SANE_MAX_LONGITUDE)
                longitude = -180;

            _data = ANTARCTIC_SHIFT_ENCODE + (ulong)(
                (long)Math.Round(latitude * GOOD_MUL) * SANE_MUL_360 + (long)Math.Round(longitude * SANE_MUL));
        }
        else
        {
            if (longitude >= ROUGH_MAX_LONGITUDE)
                longitude = -180;

            _data = SOUTHERN_SHIFT_ENCODE + (ulong)(
                (long)Math.Round(latitude * GOOD_MUL) * ROUGH_MUL_360 + (long)Math.Round(longitude * ROUGH_MUL));
        }
    }

    public (double Latitude, double Longitude) Coordinates
    {
        get
        {
            var data = _data;
            double latitude;
            double longitude;

            if (data >= CENTRAL_MIN_VALUE)
            {
                if (data < AURORA_MIN_VALUE)
                {
                    data -= CENTRAL_SHIFT_DECODE;
                    latitude = (long)(data / EXACT_MUL_360 - CENTRAL_SUB_DECODE) / EXACT_MUL;
                    longitude = (long)(data % EXACT_MUL_360 - EXACT_MUL_180) / EXACT_MUL;
                }
                else if (data < ARCTIC_MIN_VALUE)
                {
                    data -= AURORA_SHIFT_DECODE;
                    latitude = (data / GOOD_MUL_360 + AURORA_ADD_DECODE) / EXACT_MUL;
                    longitude = (long)(data % GOOD_MUL_360 - GOOD_MUL_180) / GOOD_MUL;
                }
                else if (data < NORTHERN_MIN_VALUE)
                {
                    data -= ARCTIC_SHIFT_DECODE;
                    latitude = (data / SANE_MUL_360 + ARCTIC_ADD_DECODE) / GOOD_MUL;
                    longitude = (long)(data % SANE_MUL_360 - SANE_MUL_180) / SANE_MUL;
                }
                else if (data <= MAX_VALUE)
                {
                    data -= NORTHERN_SHIFT_DECODE;
                    latitude = (data / ROUGH_MUL_360 + NORTHERN_ADD_DECODE) / GOOD_MUL;
                    longitude = (long)(data % ROUGH_MUL_360 - ROUGH_MUL_180) / ROUGH_MUL;
                }
                else
                    throw new InvalidOperationException("Incorrect data.");
            }
            else if (data >= INTERIM_MIN_VALUE)
            {
                data -= INTERIM_SHIFT_DECODE;
                latitude = (long)(data / GOOD_MUL_360 - INTERIM_SUB_DECODE) / GOOD_MUL;
                longitude = (long)(data % GOOD_MUL_360 - GOOD_MUL_180) / GOOD_MUL;
            }
            else if (data >= ANTARCTIC_MIN_VALUE)
            {
                data -= ANTARCTIC_SHIFT_DECODE;
                latitude = (long)(data / SANE_MUL_360 - ANTARCTIC_SUB_DECODE) / GOOD_MUL;
                longitude = (long)(data % SANE_MUL_360 - SANE_MUL_180) / SANE_MUL;
            }
            else
            {
                latitude = (long)(data / ROUGH_MUL_360 - SOUTHERN_SUB_DECODE) / GOOD_MUL;
                longitude = (long)(data % ROUGH_MUL_360 - ROUGH_MUL_180) / ROUGH_MUL;
            }

            return (latitude, longitude);
        }
    }

    public double Latitude => Coordinates.Latitude;

    public double Longitude => Coordinates.Longitude;

    public static LatLng64 FromData(ulong data) => new(data);

    public override string ToString()
    {
        var (latitude, longitude) = Coordinates;
        return $"{latitude}, {longitude}";
    }

    #region IEquatable

    public bool Equals(LatLng64 other) => _data == other._data;

    public override bool Equals(object? obj) => obj is LatLng64 latLng && Equals(latLng);

    public override int GetHashCode() => _data.GetHashCode();

    public static bool operator ==(LatLng64 left, LatLng64 right) => left.Equals(right);

    public static bool operator !=(LatLng64 left, LatLng64 right) => !(left == right);

    #endregion
}
