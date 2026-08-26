namespace FinGrow.Domain.Common;

public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id) => Id = id;

    protected Entity() { }

    public Guid Id { get; protected set; }

    public bool Equals(Entity? other) =>
        other is not null && other.GetType() == GetType() && other.Id == Id;

    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
