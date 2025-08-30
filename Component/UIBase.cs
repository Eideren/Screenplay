using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Screenplay.Nodes;

namespace Screenplay.Component
{
    public abstract class UIBase : MonoBehaviour
    {
        public abstract bool DialogAdvancesAutomatically { get; }
        public abstract bool FastForwardRequested { get; }
        public abstract void StartLineTypewriting(string line);
        public abstract void FinishedTypewriting();
        public abstract void SetTypewritingCharacter(int characters);
        public abstract void StartDialogPresentation();
        public abstract void EndDialogPresentation();
        public abstract UniTask<Choice.Data> ChoicePresentation(Choice.Data[] choices, CancellationToken cancellation);
        public abstract void PlayChatter(AudioClip clip, Interlocutor interlocutor);
    }
}
