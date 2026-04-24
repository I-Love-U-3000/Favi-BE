namespace Favi_BE.BuildingBlocks.Domain;

public sealed class BusinessRuleValidationException : Exception
{
    public BusinessRuleValidationException(string message) : base(message)
    {
    }
}
