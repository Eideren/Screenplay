using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    [NodeVisuals(Icon = "d_console.infoicon")]
    public class Dialog : ExecutableLinear, ILocalizableNode, IInterlocutorSource
    {
        [InlineProperty, HideLabel]
        public LocalizableText Line = new("Dialog Line\n\nAnother Line");

        [Input(Stroke = NoodleStroke.Dashed), SerializeReference]
        public IInterlocutorSource? InterlocutorSource;

        public UniTask<IInterlocutor?> GetInterlocutor(IEventContext context, CancellationToken cancellationToken)
        {
            return InterlocutorSource?.GetInterlocutor(context, cancellationToken) ?? new UniTask<IInterlocutor?>(null);
        }

        IEnumerable<string> Lines()
        {
            foreach (string s in Line.Content.Split("\n\n"))
            {
                var trimmed = s.Trim();
                if (trimmed.Length == 0)
                    continue;
                yield return trimmed;
            }
        }

        public IEnumerable<LocalizableText> GetTextInstances()
        {
            yield return Line;
        }

        public override void CollectReferences(ReferenceCollector references) { }

        public override UniTask Persistence(IEventContext context, CancellationToken cancellationToken) => UniTask.CompletedTask;

        protected override UniTask LinearExecution(IEventContext context, CancellationToken cancellation) => RunDialog(context, cancellation, false);

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (fastForwarded == false)
            {
                previewer.AddCustomPreview(cs => RunDialog(previewer, cs, true));
            }
            else
            {
                var ui = previewer.GetDialogUI();
                if (Lines().LastOrDefault() is { } lastLine)
                {
                    ui.StartDialogPresentation();
                    ui.StartLineTypewriting(lastLine);
                    ui.SetTypewritingCharacter(lastLine.Length);
                    ui.FinishedTypewriting();
                    ui.EndDialogPresentation();
                }
            }
        }

        private async UniTask RunDialog(IEventContext context, CancellationToken cancellation, bool previewMode)
        {
            var interlocutor = await GetInterlocutor(context, cancellation);
            if (interlocutor is not null)
            {
                await interlocutor.RunDialog(context, Lines(), previewMode, cancellation);
                return;
            }

            await IInterlocutor.DefaultRunDialog(context, Lines(), previewMode, cancellation);
        }
    }
}
