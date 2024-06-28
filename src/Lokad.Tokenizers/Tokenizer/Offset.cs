// Port notes:
// - OffsetSize is ported as 'uint'
// - Token and TokenRef have been merged as 'Token'.

namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Offset information (in unicode points) to relate a token back to its original input string
/// </summary>
public class Offset : IEquatable<Offset>
{
    public uint Begin { get; set; }
    public uint End { get; set; }

    /// <summary>
    /// Create a new offset from a begin and end positions
    /// </summary>
    public Offset(uint begin, uint end)
    {
        Begin = begin;
        End = end;
    }

    /// <summary>
    /// Wrap the offset into an option
    /// </summary>
    public Offset? IntoOption()
    {
        if (End > Begin)
        {
            return this;
        }
        else
        {
            return null;
        }
    }

    public override String ToString() => $"({Begin}:{End})";

    public bool Equals(Offset? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Begin == other.Begin && End == other.End;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((Offset)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Begin * 397) ^ (int)End;
        }
    }

    public static bool operator ==(Offset? left, Offset? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Offset? left, Offset? right)
    {
        return !Equals(left, right);
    }
}
