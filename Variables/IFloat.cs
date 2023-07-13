namespace Screenplay.Variables
{
    public interface IFloat : IValue<float>, INumber
    {
        double INumber.GetNumber() => Value;
    }
}