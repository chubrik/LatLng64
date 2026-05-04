namespace Chubrik.LatLng64.Tests;

using System;

public class DefaultStateTests
{
    [Fact]
    public void GetCoordinates_OnDefault_ThrowsInvalidOperation()
    {
        var def = default(LatLng64);
        Assert.Throws<InvalidOperationException>(() => def.GetCoordinates());
    }

    [Fact]
    public void Deconstruct_OnDefault_ThrowsInvalidOperation()
    {
        var def = default(LatLng64);
        Assert.Throws<InvalidOperationException>(() => def.Deconstruct(out _, out _));
    }

    [Fact]
    public void ToString_OnDefault_ThrowsInvalidOperation()
    {
        var def = default(LatLng64);
        Assert.Throws<InvalidOperationException>(() => def.ToString());
    }

    [Fact]
    public void Data_OnDefault_IsZero()
    {
        Assert.Equal(0ul, default(LatLng64).Data);
    }

    [Fact]
    public void Equals_DefaultEqualsDefault()
    {
        Assert.True(default(LatLng64).Equals(default(LatLng64)));
    }

    [Fact]
    public void DefaultIsNotEqualToValidZeroZero()
    {
        // (0, 0) is a valid coordinate; its encoded data is non-zero.
        Assert.NotEqual(default, new LatLng64(0.0, 0.0));
    }

#if NET
    [Fact]
    public void TryFormat_OnDefault_ThrowsInvalidOperation()
    {
        // stackalloc is illegal in lambda; wrap in a static local.
        static void Act()
        {
            Span<char> buffer = stackalloc char[64];
            default(LatLng64).TryFormat(buffer, out _);
        }

        Assert.Throws<InvalidOperationException>(Act);
    }
#endif
}
