namespace Screenplay.Variables
{
    public interface IFloat : IValue<float>, INumber
    {
        decimal INumber.GetNumber() => (decimal)Value;
    }
}