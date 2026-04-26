using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using YNode;

namespace Screenplay.Nodes
{
    [NodeVisuals(Icon = "d_AudioClip On Icon")]
    public class MusicFadeOut : ExecutableLinear
    {
        [Unit(Units.Second)]
        public float TransitionDuration;

        public override void CollectReferences(ReferenceCollector references) { }

        protected override UniTask LinearExecution(IEventContext context, Cancellation cancellation)
        {
            return UniTask.CompletedTask;
        }

        public override UniTask Persistence(IEventContext context, Cancellation cancellation)
        {
            if (Music.CurrentPlaying != null)
            {
                Music.TransitionOut(Music.CurrentPlaying, TransitionDuration, cancellation).Forget();
            }

            return UniTask.CompletedTask;
        }

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {

        }
    }
}
