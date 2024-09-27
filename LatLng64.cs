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

    private const double EXACT_ERROR = 0.5 / EXACT_MUL;  //  0.000000025
    private const double GOOD_ERROR = 0.5 / GOOD_MUL;    //  0.00000005
    private const double SANE_ERROR = 0.5 / SANE_MUL;    //  0.0000001
    private const double ROUGH_ERROR = 0.5 / ROUGH_MUL;  //  0.0000005

    private const double DOUBLE_BIT_ERROR = 1e-14;                                        //    0.00000000000001
    private const double EXACT_MAX_LONGITUDE = 180 - EXACT_ERROR - 2 * DOUBLE_BIT_ERROR;  //  179.99999997499998
    private const double GOOD_MAX_LONGITUDE = 180 - GOOD_ERROR - 2 * DOUBLE_BIT_ERROR;    //  179.99999994999996
    private const double SANE_MAX_LONGITUDE = 180 - SANE_ERROR - 2 * DOUBLE_BIT_ERROR;    //  179.99999989999998
    private const double ROUGH_MAX_LONGITUDE = 180 - ROUGH_ERROR - 2 * DOUBLE_BIT_ERROR;  //  179.99999949999997

    private const ulong NORTHERN_SIZE = ((NORTHERN_TOP - NORTHERN_BOTTOM) * (ulong)GOOD_MUL + 1) * ROUGH_MUL_360;
    private const ulong ARCTIC_SIZE = (NORTHERN_BOTTOM - ARCTIC_BOTTOM) * (ulong)GOOD_MUL * SANE_MUL_360;
    private const ulong AURORA_SIZE = (ARCTIC_BOTTOM - AURORA_BOTTOM) * (ulong)EXACT_MUL * GOOD_MUL_360;
    private const ulong CENTRAL_SIZE = ((AURORA_BOTTOM - CENTRAL_BOTTOM) * (ulong)EXACT_MUL - 1) * EXACT_MUL_360;
    private const ulong INTERIM_SIZE = (CENTRAL_BOTTOM - INTERIM_BOTTOM) * (ulong)GOOD_MUL * GOOD_MUL_360;
    private const ulong ANTARCTIC_SIZE = (INTERIM_BOTTOM - ANTARCTIC_BOTTOM) * (ulong)GOOD_MUL * SANE_MUL_360;
    private const ulong SOUTHERN_SIZE = ((ANTARCTIC_BOTTOM - SOUTHERN_BOTTOM) * (ulong)GOOD_MUL + 1) * ROUGH_MUL_360;

    private const ulong NORTHERN_MAX_DATA = NORTHERN_MIN_DATA + NORTHERN_SIZE - 1;  //  18 432 000 000 359 999 999   0x_FFCB_9E57_E975_29FF
    private const ulong NORTHERN_MIN_DATA = ARCTIC_MIN_DATA + ARCTIC_SIZE;          //  18 414 000 000 000 000 000   0x_FF8B_AB70_3E0B_0000
    private const ulong ARCTIC_MIN_DATA = AURORA_MIN_DATA + AURORA_SIZE;            //  18 180 000 000 000 000 000   0x_FC4C_55AD_A09A_0000
    private const ulong AURORA_MIN_DATA = CENTRAL_MIN_DATA + CENTRAL_SIZE;          //  17 316 000 000 000 000 000   0x_F04E_CA41_82AA_0000
    private const ulong CENTRAL_MIN_DATA = INTERIM_MIN_DATA + INTERIM_SIZE;         //     612 000 007 200 000 000   0x_087E_42C3_97B1_4800
    private const ulong INTERIM_MIN_DATA = ANTARCTIC_MIN_DATA + ANTARCTIC_SIZE;     //     468 000 007 200 000 000   0x_067E_AB86_E809_4800
    private const ulong ANTARCTIC_MIN_DATA = SOUTHERN_MIN_DATA + SOUTHERN_SIZE;     //      18 000 007 200 000 000   0x_003F_F2E9_431C_4800
    private const ulong SOUTHERN_MIN_DATA = 6_840_000_000;                          //               6 840 000 000   0x_0000_0001_97B2_1E00

    private const double NORTHERN_BOTTOM_ENCODE = NORTHERN_BOTTOM - GOOD_ERROR;                    //  84.99999995
    private const double ARCTIC_BOTTOM_ENCODE = ARCTIC_BOTTOM - EXACT_ERROR;                       //  71.999999975
    private const double AURORA_BOTTOM_ENCODE = AURORA_BOTTOM - EXACT_ERROR - DOUBLE_BIT_ERROR;    //  59.999999974999994
    private const double CENTRAL_BOTTOM_ENCODE = CENTRAL_BOTTOM + EXACT_ERROR + DOUBLE_BIT_ERROR;  // -55.999999974999994
    private const double INTERIM_BOTTOM_ENCODE = INTERIM_BOTTOM + GOOD_ERROR + DOUBLE_BIT_ERROR;   // -59.999999949999996
    private const double ANTARCTIC_BOTTOM_ENCODE = ANTARCTIC_BOTTOM + GOOD_ERROR;                  // -84.99999995

    private const ulong NORTHERN_SHIFT_ENCODE = NORTHERN_MIN_DATA - NORTHERN_BOTTOM * (ulong)GOOD_MUL * ROUGH_MUL_360 + ROUGH_MUL_180;
    private const ulong ARCTIC_SHIFT_ENCODE = ARCTIC_MIN_DATA - ARCTIC_BOTTOM * (ulong)GOOD_MUL * SANE_MUL_360 + SANE_MUL_180;
    private const ulong AURORA_SHIFT_ENCODE = AURORA_MIN_DATA - AURORA_BOTTOM * (ulong)EXACT_MUL * GOOD_MUL_360 + GOOD_MUL_180;
    private const ulong CENTRAL_SHIFT_ENCODE = CENTRAL_MIN_DATA + (-CENTRAL_BOTTOM * (ulong)EXACT_MUL - 1) * EXACT_MUL_360 + EXACT_MUL_180;
    private const ulong INTERIM_SHIFT_ENCODE = INTERIM_MIN_DATA + (-INTERIM_BOTTOM * (ulong)GOOD_MUL - 1) * GOOD_MUL_360 + GOOD_MUL_180;
    private const ulong ANTARCTIC_SHIFT_ENCODE = ANTARCTIC_MIN_DATA + (-ANTARCTIC_BOTTOM * (ulong)GOOD_MUL - 1) * SANE_MUL_360 + SANE_MUL_180;
    private const ulong SOUTHERN_SHIFT_ENCODE = SOUTHERN_MIN_DATA + (-SOUTHERN_BOTTOM) * (ulong)GOOD_MUL * ROUGH_MUL_360 + ROUGH_MUL_180;

    private const ulong NORTHERN_SHIFT_DECODE = NORTHERN_MIN_DATA;
    private const ulong ARCTIC_SHIFT_DECODE = ARCTIC_MIN_DATA;
    private const ulong AURORA_SHIFT_DECODE = AURORA_MIN_DATA;
    private const ulong CENTRAL_SHIFT_DECODE = CENTRAL_MIN_DATA - EXACT_MUL_360;
    private const ulong INTERIM_SHIFT_DECODE = INTERIM_MIN_DATA - GOOD_MUL_360;
    private const ulong ANTARCTIC_SHIFT_DECODE = ANTARCTIC_MIN_DATA - SANE_MUL_360;
    private const ulong SOUTHERN_SHIFT_DECODE = SOUTHERN_MIN_DATA;

    private const ulong NORTHERN_ADD_DECODE = (ulong)(NORTHERN_BOTTOM * GOOD_MUL);
    private const ulong ARCTIC_ADD_DECODE = (ulong)(ARCTIC_BOTTOM * GOOD_MUL);
    private const ulong AURORA_ADD_DECODE = (ulong)(AURORA_BOTTOM * EXACT_MUL);
    private const ulong CENTRAL_SUB_DECODE = (ulong)(-CENTRAL_BOTTOM * EXACT_MUL);
    private const ulong INTERIM_SUB_DECODE = (ulong)(-INTERIM_BOTTOM * GOOD_MUL);
    private const ulong ANTARCTIC_SUB_DECODE = (ulong)(-ANTARCTIC_BOTTOM * GOOD_MUL);
    private const ulong SOUTHERN_SUB_DECODE = (ulong)(-SOUTHERN_BOTTOM * GOOD_MUL);

    #endregion

    private readonly ulong _data;

    private LatLng64(ulong data)
    {
        if (data < SOUTHERN_MIN_DATA || data > NORTHERN_MAX_DATA)
            throw new ArgumentOutOfRangeException(nameof(data));

        _data = data;
    }

    public LatLng64(double latitude, double longitude)
    {
        if (!(latitude >= -90 && latitude <= 90))
            throw new ArgumentOutOfRangeException(nameof(latitude),
                $"Requires a number between -90 and +90 inclusive, but the actual value is: {latitude}");

        if (!(longitude >= -180 && longitude <= 180))
            throw new ArgumentOutOfRangeException(nameof(longitude),
                $"Requires a number between -180 and +180 inclusive, but the actual value is: {longitude}");

        unchecked
        {
            if (latitude > CENTRAL_BOTTOM_ENCODE)
            {
                if (latitude < AURORA_BOTTOM_ENCODE)
                {
                    if (longitude > EXACT_MAX_LONGITUDE)
                        longitude = -180;

                    _data = CENTRAL_SHIFT_ENCODE + (ulong)(
                        (long)Math.Round(latitude * EXACT_MUL) * EXACT_MUL_360 + (long)Math.Round(longitude * EXACT_MUL));
                }
                else if (latitude < ARCTIC_BOTTOM_ENCODE)
                {
                    if (longitude > GOOD_MAX_LONGITUDE)
                        longitude = -180;

                    _data = AURORA_SHIFT_ENCODE + (ulong)(
                        (long)Math.Round(latitude * EXACT_MUL) * GOOD_MUL_360 + (long)Math.Round(longitude * GOOD_MUL));
                }
                else if (latitude < NORTHERN_BOTTOM_ENCODE)
                {
                    if (longitude > SANE_MAX_LONGITUDE)
                        longitude = -180;

                    _data = ARCTIC_SHIFT_ENCODE + (ulong)(
                        (long)Math.Round(latitude * GOOD_MUL) * SANE_MUL_360 + (long)Math.Round(longitude * SANE_MUL));
                }
                else
                {
                    if (longitude > ROUGH_MAX_LONGITUDE)
                        longitude = -180;

                    _data = NORTHERN_SHIFT_ENCODE + (ulong)(
                        (long)Math.Round(latitude * GOOD_MUL) * ROUGH_MUL_360 + (long)Math.Round(longitude * ROUGH_MUL));
                }
            }
            else if (latitude > INTERIM_BOTTOM_ENCODE)
            {
                if (longitude > GOOD_MAX_LONGITUDE)
                    longitude = -180;

                _data = INTERIM_SHIFT_ENCODE + (ulong)(
                    (long)Math.Round(latitude * GOOD_MUL) * GOOD_MUL_360 + (long)Math.Round(longitude * GOOD_MUL));
            }
            else if (latitude > ANTARCTIC_BOTTOM_ENCODE)
            {
                if (longitude > SANE_MAX_LONGITUDE)
                    longitude = -180;

                _data = ANTARCTIC_SHIFT_ENCODE + (ulong)(
                    (long)Math.Round(latitude * GOOD_MUL) * SANE_MUL_360 + (long)Math.Round(longitude * SANE_MUL));
            }
            else
            {
                if (longitude > ROUGH_MAX_LONGITUDE)
                    longitude = -180;

                _data = SOUTHERN_SHIFT_ENCODE + (ulong)(
                    (long)Math.Round(latitude * GOOD_MUL) * ROUGH_MUL_360 + (long)Math.Round(longitude * ROUGH_MUL));
            }
        }
    }

    public (double Latitude, double Longitude) GetCoordinates()
    {
        double latitude;
        double longitude;

        unchecked
        {
            if (_data >= CENTRAL_MIN_DATA)
            {
                if (_data < AURORA_MIN_DATA)
                {
                    var (quotinent, reminder) = Math.DivRem(_data - CENTRAL_SHIFT_DECODE, EXACT_MUL_360);
                    latitude = (long)(quotinent - CENTRAL_SUB_DECODE) / EXACT_MUL;
                    longitude = (long)(reminder - EXACT_MUL_180) / EXACT_MUL;
                }
                else if (_data < ARCTIC_MIN_DATA)
                {
                    var (quotinent, reminder) = Math.DivRem(_data - AURORA_SHIFT_DECODE, GOOD_MUL_360);
                    latitude = (quotinent + AURORA_ADD_DECODE) / EXACT_MUL;
                    longitude = (long)(reminder - GOOD_MUL_180) / GOOD_MUL;
                }
                else if (_data < NORTHERN_MIN_DATA)
                {
                    var (quotinent, reminder) = Math.DivRem(_data - ARCTIC_SHIFT_DECODE, SANE_MUL_360);
                    latitude = (quotinent + ARCTIC_ADD_DECODE) / GOOD_MUL;
                    longitude = (long)(reminder - SANE_MUL_180) / SANE_MUL;
                }
                else if (_data <= NORTHERN_MAX_DATA)
                {
                    var (quotinent, reminder) = Math.DivRem(_data - NORTHERN_SHIFT_DECODE, ROUGH_MUL_360);
                    latitude = (quotinent + NORTHERN_ADD_DECODE) / GOOD_MUL;
                    longitude = (long)(reminder - ROUGH_MUL_180) / ROUGH_MUL;
                }
                else
                    throw new InvalidOperationException("Incorrect data.");
            }
            else if (_data >= INTERIM_MIN_DATA)
            {
                var (quotinent, reminder) = Math.DivRem(_data - INTERIM_SHIFT_DECODE, GOOD_MUL_360);
                latitude = (long)(quotinent - INTERIM_SUB_DECODE) / GOOD_MUL;
                longitude = (long)(reminder - GOOD_MUL_180) / GOOD_MUL;
            }
            else if (_data >= ANTARCTIC_MIN_DATA)
            {
                var (quotinent, reminder) = Math.DivRem(_data - ANTARCTIC_SHIFT_DECODE, SANE_MUL_360);
                latitude = (long)(quotinent - ANTARCTIC_SUB_DECODE) / GOOD_MUL;
                longitude = (long)(reminder - SANE_MUL_180) / SANE_MUL;
            }
            else if (_data >= SOUTHERN_MIN_DATA)
            {
                var (quotinent, reminder) = Math.DivRem(_data - SOUTHERN_SHIFT_DECODE, ROUGH_MUL_360);
                latitude = (long)(quotinent - SOUTHERN_SUB_DECODE) / GOOD_MUL;
                longitude = (long)(reminder - ROUGH_MUL_180) / ROUGH_MUL;
            }
            else
                throw new InvalidOperationException("Incorrect data.");
        }

        return (latitude, longitude);
    }

    public static LatLng64 FromData(ulong data)
    {
        return new LatLng64(data);
    }

    public override string ToString()
    {
        var (latitude, longitude) = GetCoordinates();
        return $"{{{latitude}, {longitude}}}";
    }

    #region IEquatable

    public bool Equals(LatLng64 other) => _data == other._data;

    public override bool Equals(object? obj) => obj is LatLng64 latLng && Equals(latLng);

    public override int GetHashCode() => _data.GetHashCode();

    public static bool operator ==(LatLng64 left, LatLng64 right) => left.Equals(right);

    public static bool operator !=(LatLng64 left, LatLng64 right) => !(left == right);

    #endregion
}
