namespace Favi_BE.BuildingBlocks.Domain;

public sealed class BusinessRuleValidationException : Exception
{
    public IBusinessRule? BrokenRule { get; }

    public BusinessRuleValidationException(string message) : base(message)
    {
    }

    public BusinessRuleValidationException(IBusinessRule brokenRule) : base(brokenRule.Message)
    {
        BrokenRule = brokenRule;
    }
}
