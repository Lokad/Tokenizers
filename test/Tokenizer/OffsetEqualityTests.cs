using Lokad.Tokenizers.Tokenizer;
using Xunit;

namespace Lokad.Tokenizers.Tests.Tokenizer;

public sealed class OffsetEqualityTests
{
    private sealed class Derived(uint begin, uint end) : Offset(begin, end);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void EqualityUsesCoordinatesAndRuntimeTypeConsistently(bool leftDerived, bool rightDerived)
    {
        Offset left = leftDerived ? new Derived(1, 2) : new Offset(1, 2);
        Offset right = rightDerived ? new Derived(1, 2) : new Offset(1, 2);
        bool equal = leftDerived == rightDerived;
        Assert.Equal(equal, left.Equals(right));
        Assert.Equal(equal, left.Equals((object)right));
        Assert.Equal(equal, left == right);
        Assert.Equal(!equal, left != right);
        Assert.Equal(equal, new HashSet<Offset> { left }.Contains(right));
        Assert.Equal(equal, new HashSet<object> { left }.Contains(right));
        Assert.False(left.Equals((Offset?)null));
        Assert.False(left == null);
        Assert.False(left.Equals(new Offset(2, 3)));
        if (equal) Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }
}
