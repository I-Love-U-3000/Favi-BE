namespace Favi_BE.BuildingBlocks.Domain;

public interface IBusinessRule
{
    bool IsBroken();
    string Message { get; }
}
