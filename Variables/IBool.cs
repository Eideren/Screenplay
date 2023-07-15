namespace Screenplay.Variables
{
    public interface IBool : IValue<bool>, INumber
    {
        decimal INumber.GetNumber() => Value ? 1 : 0;
    }
}