namespace ClearMeasure.Bootcamp.Core.Model;

public abstract class EntityBase<T> : IEquatable<T> where T : EntityBase<T>, new()
{
    public abstract Guid Id { get; set; }

    public bool Equals(T? other) =>
        other is not null && IsSameIdentity(other);

    public override bool Equals(object? obj) =>
        obj is T typed && typed.GetType() == GetType() && IsSameIdentity(typed);

    private bool IsSameIdentity(T other) =>
        ReferenceEquals(this, other) || HasSameNonEmptyId(Id, other.Id);

    public override string ToString()
    {
        return base.ToString() + "-" + Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(EntityBase<T>? left, EntityBase<T>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EntityBase<T>? left, EntityBase<T>? right)
    {
        return !Equals(left, right);
    }

    private static bool HasSameNonEmptyId(Guid left, Guid right) =>
        left.Equals(right) && !left.Equals(Guid.Empty);
}
