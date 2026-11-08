namespace Lokad.Tokenizers.Tokenizer;

/// <summary>
/// Offset information (in unicode points) to relate a token back to its original input string.
/// Equality requires equal coordinates and the same runtime type.
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
    /// Return this offset for a nonempty span, or null for an empty or reversed span.
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

        return GetType() == other.GetType() && Begin == other.Begin && End == other.End;
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

        return obj is Offset other && Equals(other);
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
