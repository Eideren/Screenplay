using Screenplay.Nodes;

namespace Screenplay
{
    public interface IPreconditionCollector
    {
        Locals SharedLocals { get; }
        LatentVariable<bool> IsBusy { get; }
        void SetUnlockedState(bool state, params (ILocal id, guid value)[] locals);
    }
}
