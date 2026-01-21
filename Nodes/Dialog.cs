using System;
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

        public UniTask<Interlocutor?> GetInterlocutor(IEventContext context, CancellationToken cancellationToken) => InterlocutorSource?.GetInterlocutor(context, cancellationToken) ?? new UniTask<Interlocutor?>(null);

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

        public override void FastForward(IEventContext context, CancellationToken cancellationToken) { }

        protected override UniTask LinearExecution(IEventContext context, CancellationToken cancellation) => RunDialog(context, cancellation, false);

        public override void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (fastForwarded == false)
                previewer.AddCustomPreview(cs => RunDialog(previewer, cs, true));

            if (previewer.GetDialogUI() is {} ui && Lines().LastOrDefault() is {} lastLine)
            {
                ui.StartDialogPresentation();
                ui.StartLineTypewriting(lastLine);
                ui.SetTypewritingCharacter(lastLine.Length);
                ui.FinishedTypewriting();
                ui.EndDialogPresentation();
            }
        }

        private async UniTask RunDialog(IEventContext context, CancellationToken cancellation, bool previewMode)
        {
            if (context.GetDialogUI() is {} ui == false)
            {
                Debug.LogWarning($"{nameof(ScreenplayGraph.DialogUIPrefab)} has not been set, no interface to present this {nameof(Dialog)} on");
                return;
            }

            var interlocutor = await GetInterlocutor(context, cancellation);
            ui.StartDialogPresentation();
            foreach (var text in Lines())
            {
                ui.StartLineTypewriting(text);
                ui.SetTypewritingCharacter(0);
                float time = 0f;
                int lastChatter = 0;
                for (int i = 0; i < text.Length; i++)
                {
                    ui.SetTypewritingCharacter(i + 1);

                    if (i + 1 == text.Length)
                        break; // Don't delay for the last character

                    if (interlocutor != null && i - lastChatter >= interlocutor.CharactersPerChatter)
                        Chatter(ref lastChatter, i, text, interlocutor, ui);

                    time += interlocutor?.GetDuration(text[i]) ?? 0.1f;
                    for (; time > 0f; time -= Time.unscaledDeltaTime)
                    {
                        if (ui.FastForwardRequested)
                        {
                            await UniTask.NextFrame(cancellation, cancelImmediately:true);
                            goto BREAK_TYPEWRITING;
                        }

                        await UniTask.NextFrame(cancellation, cancelImmediately:true);
                    }
                }

                if (interlocutor != null)
                    Chatter(ref lastChatter, text.Length - 1, text, interlocutor, ui);

                BREAK_TYPEWRITING:
                ui.SetTypewritingCharacter(text.Length);
                ui.FinishedTypewriting();

                while (previewMode || ui.DialogAdvancesAutomatically == false)
                {
                    if (ui.FastForwardRequested)
                    {
                        await UniTask.NextFrame(cancellation, cancelImmediately:true);
                        break;
                    }

                    await UniTask.NextFrame(cancellation, cancelImmediately:true);
                }
            }
            ui.EndDialogPresentation();
        }

        private void Chatter(ref int last, int current, string text, Interlocutor interlocutor, Component.UIBase ui)
        {
            int hash = 0;
            int processed = 0;
            for (; last <= current; last++)
            {
                if (interlocutor.GetDuration(text[last]) == 0f)
                {
                    for (; last <= current && interlocutor.GetDuration(text[last]) == 0f; last++) { }
                    break;
                }

                hash = HashCode.Combine(hash, text[last]);
                processed++;
            }

            if (interlocutor.Chatter.Length == 0 || processed == 0)
                return;

            var index = hash % interlocutor.Chatter.Length;
            index = index < 0 ? interlocutor.Chatter.Length + index : index;
            var chatter = interlocutor.Chatter[index];
            ui.PlayChatter(chatter, interlocutor);
        }
    }
}
