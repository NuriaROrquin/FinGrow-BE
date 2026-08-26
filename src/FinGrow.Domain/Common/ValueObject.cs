namespace FinGrow.Domain.Common;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other) =>
        other is not null
        && other.GetType() == GetType()
        && other.GetEqualityComponents().SequenceEqual(GetEqualityComponents());

    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(default(HashCode), (hash, component) =>
            {
                hash.Add(component);
                return hash;
            })
            .ToHashCode();

    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
}
