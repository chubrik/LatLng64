namespace Chubrik.LatLng64.Tests;

using System;

public class EquatableComparableTests
{
    private static readonly LatLng64 A = new(55.7558, 37.6173);
    private static readonly LatLng64 B = new(55.7558, 37.6173);
    private static readonly LatLng64 C = new(40.7128, -74.0060);

    [Fact]
    public void Equals_SameCoordinates_ReturnsTrue()
    {
        Assert.True(A.Equals(B));
    }

    [Fact]
    public void Equals_DifferentCoordinates_ReturnsFalse()
    {
        Assert.False(A.Equals(C));
    }

    [Fact]
    public void EqualsObject_SameType_DelegatesToTyped()
    {
        Assert.True(A.Equals((object)B));
        Assert.False(A.Equals((object)C));
    }

    [Fact]
    public void EqualsObject_NullOrOtherType_ReturnsFalse()
    {
        Assert.False(A.Equals(null));
        Assert.False(A.Equals("not a coord"));
        Assert.False(A.Equals(42));
    }

    [Fact]
    public void GetHashCode_EqualInstances_ReturnSameHash()
    {
        Assert.Equal(A.GetHashCode(), B.GetHashCode());
    }

    [Fact]
    public void EqualityOperators_BehaveLikeEquals()
    {
        Assert.True(A == B);
        Assert.False(A != B);
        Assert.False(A == C);
        Assert.True(A != C);
    }

    [Fact]
    public void CompareTo_Generic_ZeroForEqual()
    {
        Assert.Equal(0, A.CompareTo(B));
    }

    [Fact]
    public void CompareTo_Generic_MatchesDataOrdering()
    {
        Assert.Equal(Math.Sign(A.Data.CompareTo(C.Data)), Math.Sign(A.CompareTo(C)));
        Assert.Equal(Math.Sign(C.Data.CompareTo(A.Data)), Math.Sign(C.CompareTo(A)));
    }

    [Fact]
    public void CompareTo_NonGeneric_SameType_DelegatesToTyped()
    {
        Assert.Equal(0, ((IComparable)A).CompareTo(B));
        Assert.Equal(Math.Sign(A.CompareTo(C)),
            Math.Sign(((IComparable)A).CompareTo(C)));
    }

    [Fact]
    public void CompareTo_NonGeneric_Null_ReturnsPositive()
    {
        Assert.Equal(1, ((IComparable)A).CompareTo(null));
    }

    [Fact]
    public void CompareTo_NonGeneric_WrongType_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ((IComparable)A).CompareTo("not a coord"));
        Assert.Throws<ArgumentException>(() => ((IComparable)A).CompareTo(42));
    }
}
