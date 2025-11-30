using Cysharp.Threading.Tasks;

namespace Screenplay
{
    public class EventRunnerState
    {
        public readonly AutoResetUniTaskCompletionSource StateChanged = AutoResetUniTaskCompletionSource.Create();
        public bool IsRunningEvent { get; set; }
    }
}
