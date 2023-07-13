namespace Screenplay.Variables
{
    public interface IBool : IValue<bool>, INumber
    {
        double INumber.GetNumber() => Value ? 1.0 : 0.0;
    }
}