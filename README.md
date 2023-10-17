# LatLng64
The `LatLng64` is a 64-bit structure that compactly contains latitude and longitude data with an accuracy of ±2.8 mm.
This format is great for geographic databases. Occupies half the space of traditional solutions
and provides accuracy sufficient for the vast majority of applications.
<br><br>

### Features
* The structure occupies 64 bits of memory and uses them to the maximum.
* In the populated part of the Earth, the accuracy is no worse than ±2.8 mm.
* In the polar regions of the Earth, the accuracy is no worse than ±5.6 mm.
* The restored latitude and longitude values have a neat decimal form.
<br><br>

### Comparison with other formats
* The **Float32** pair has different precision depending on the location.
  Step up to 1.7 m is unacceptable for most applications.
* The **Float64** pair is the traditional solution.
  But it takes up a huge 128 bits and gives meaningless sub-nanometer precision.
* The **Int32** pair can give uniform accuracy of ±5.6 mm. This is still twice as bad as `LatLng64`.
