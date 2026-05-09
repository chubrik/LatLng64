# LatLng64

[![NuGet](https://img.shields.io/nuget/v/Chubrik.LatLng64)](https://www.nuget.org/packages/Chubrik.LatLng64)
[![CI](https://github.com/chubrik/LatLng64/actions/workflows/ci.yml/badge.svg)](https://github.com/chubrik/LatLng64/actions/workflows/ci.yml)

A 64-bit struct that packs [latitude, longitude] into a single `ulong` with ±2.8 mm precision
in populated latitudes and ±5.6 mm in polar zones. Decodes to clean decimals. Half the size
of a `(double, double)` pair; ideal for geographic databases.

## Install

```
dotnet add package Chubrik.LatLng64
```

Targets `net8.0` and `netstandard2.0`. Both builds implement `IEquatable<LatLng64>`, `IComparable`,
`IComparable<LatLng64>`, and `IFormattable`. The `net8.0` build adds `ISpanFormattable`,
`IParsable<LatLng64>`, `ISpanParsable<LatLng64>`, `IUtf8SpanFormattable`, and
`IUtf8SpanParsable<LatLng64>`.

## Quick start

```csharp
using Chubrik.LatLng64;

var coord = new LatLng64(55.7558, 37.6173);
var (lat, lng) = coord.GetCoordinates();

// Persist as a single 64-bit value (great for a database column)
ulong raw = coord.Data;
var restored = LatLng64.FromData(raw);
```

## API at a glance

| Member                              | Purpose                                  |
|-------------------------------------|------------------------------------------|
| `new LatLng64(lat, lng)`            | Construct from degrees                   |
| `GetCoordinates()`                  | Decode to `(double, double)`             |
| `Data` / `FromData(ulong)`          | Serialize / deserialize as `ulong`       |
| `Parse` / `TryParse`                | Convert from text                        |
| `ToString` / `TryFormat`            | Convert to text                          |
| `Equals`, `==`, `!=`, `CompareTo`   | Equality (by encoding) and ordering      |

## Database storage

The `Data` property is a stable `ulong` — bytes are platform-independent and round-trip on any
process. Entity Framework Core:

```csharp
modelBuilder.Entity<Place>()
    .Property(p => p.Location)
    .HasConversion(v => v.Data, raw => LatLng64.FromData(raw));
```

## JSON serialization

`System.Text.Json` doesn’t serialize `LatLng64` out of the box. A small converter handles it —
`Data` is wrapped as a JSON string to keep precision through JavaScript’s `Number` type
(values exceed 2^53):

```csharp
public sealed class LatLng64JsonConverter : JsonConverter<LatLng64>
{
    public override LatLng64 Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => LatLng64.FromData(ulong.Parse(reader.GetString()!, CultureInfo.InvariantCulture));

    public override void Write(
        Utf8JsonWriter writer, LatLng64 value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Data.ToString(CultureInfo.InvariantCulture));
}
```

Register it via `[JsonConverter(typeof(LatLng64JsonConverter))]` on a property, or globally via
`JsonSerializerOptions.Converters`.

**Note:** serializing a `default(LatLng64)` writes `{"Data":"0"}` and fails to deserialize back —
initialize values before persisting.

## Notes

- **`default(LatLng64)` is intentionally invalid.** Calling `GetCoordinates`, `ToString`, or
  `TryFormat` on it throws `InvalidOperationException` — uninitialized values fail loudly instead of
  silently mapping to `(0, 0)`.
- **NaN and ±Infinity are rejected.** Constructor throws `ArgumentOutOfRangeException`; `Parse`
  throws `OverflowException` (`TryParse` returns `false`). Latitude must be in `[−90, +90]`,
  longitude in `[−180, +180]`.
- **`Parse` / `ToString` are culture-sensitive.** A `null` provider resolves to
  `CultureInfo.CurrentCulture` (not `Invariant`). Cultures with a `,` decimal separator use `;` as
  the pair separator: `"55,7558; 37,6173"`. For cross-machine text round-trips, pass an explicit
  `CultureInfo` — typically `Invariant`.
- **`CompareTo` is not a spatial order.** Orders by raw `Data` — deterministic and useful for
  sorting and database indexing, but geographically close points may be far apart in this order.

## Precision by zone

The 64-bit budget is split unevenly across seven zones so that **linear** precision stays within
±2.8 mm in populated latitudes and ±5.6 mm in polar zones. Coarser angular longitude steps in
higher zones compensate for the shrinking longitudinal degree (cos lat).

| Zone        | Latitude range | Latitude step | Longitude step | Precision   | Habitability  |
|-------------|----------------|---------------|----------------|-------------|---------------|
| Northern    | +85° … +90°    | 0.0000001°    | 0.000001°      | ±5.6 mm     | No            |
| Arctic      | +72° … +85°    | 0.0000001°    | 0.0000002°     | ±5.6 mm     | No            |
| **Aurora**  | +60° … +72°    | 0.00000005°   | 0.0000001°     | **±2.8 mm** | **Inhabited** |
| **Central** | −56° … +60°    | 0.00000005°   | 0.00000005°    | **±2.8 mm** | **Inhabited** |
| Interim     | −60° … −56°    | 0.0000001°    | 0.0000001°     | ±5.6 mm     | No            |
| Antarctic   | −85° … −60°    | 0.0000001°    | 0.0000002°     | ±5.6 mm     | No            |
| Southern    | −90° … −85°    | 0.0000001°    | 0.000001°      | ±5.6 mm     | No            |

The split is asymmetric on purpose: only northern mid-latitudes host populated land; the southern
counterpart is mostly open ocean, so the Antarctic zone absorbs the whole −60° … −85° range at
coarser steps.

## Comparison

| Format             | Size     | Precision                                                        |
|--------------------|----------|------------------------------------------------------------------|
| `LatLng64`         | 64 bits  | ±2.8 mm populated, ±5.6 mm polar zones (see table above)         |
| `(float, float)`   | 64 bits  | up to ±1.7 m — variable, often unacceptable                      |
| `(double, double)` | 128 bits | sub-nanometer — twice the storage, far past meaningful precision |
| `(int, int) × 10⁷` | 64 bits  | ±5.6 mm uniform — 4× fewer bins per area than populated zones    |

## License

Released under the [MIT License](LICENSE).
