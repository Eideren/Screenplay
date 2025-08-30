using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Screenplay.Component
{
    public class ScreenplayDispatcher : MonoBehaviour
    {
        private CancellationTokenSource? _existing;

        [Required] public ScreenplayGraph Screenplay = null!;

        // ReSharper disable once Unity.IncorrectMethodSignature
        private async UniTask OnEnable()
        {
            if (transform.parent != null)
                transform.parent = null;

            DontDestroyOnLoad(gameObject);
            _existing = new CancellationTokenSource();
            await Screenplay.StartExecution(_existing.Token);
        }

        private void OnDisable()
        {
            _existing?.Cancel();
            _existing?.Dispose();
            _existing = null;
        }

        private void OnDrawGizmos()
        {
            if (Screenplay == null)
                return;

            ScreenplayGizmos.Draw(Screenplay);
        }
    }
}
