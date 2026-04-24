namespace Favi_BE.BuildingBlocks.Domain;

public abstract class TypedIdValueBase : ValueObject
{
    public Guid Value { get; }

    protected TypedIdValueBase(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Typed id value cannot be empty.", nameof(value));
        }

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
