using Screenplay.Nodes;

namespace Screenplay
{
    public interface IEventTracker
    {
        EventRunnerState RunnerState { get; }
        void SetUnlockedState(bool state, params (VariantBase, guid)[] permutationValues);
    }
}
