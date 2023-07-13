namespace Screenplay.Variables
{
    public interface IInt : IValue<int>, INumber
    {
        double INumber.GetNumber() => Value;
    }
}