namespace Screenplay.Variables
{
    public interface IInt : IValue<int>, INumber
    {
        decimal INumber.GetNumber() => Value;
    }
}