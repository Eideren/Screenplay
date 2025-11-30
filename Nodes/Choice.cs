using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using YNode;

namespace Screenplay.Nodes
{
    [NodeTint(60, 60, 60)]
    public class Choice : AbstractScreenplayNode, IExecutable<IEventContext>, ILocalizableNode, IPrerequisiteVisitedSelf
    {
        [ListDrawerSettings(ShowFoldout = false), LabelText(" ")]
        public ChoiceInstance[] Choices =
        {
            new()
            {
                Text = new("Choice A"),
            },
            new()
            {
                Text = new("Choice B"),
            }
        };

        public IEnumerable<IExe<IEventContext>> Followup()
        {
            foreach (var instance in Choices)
            {
                if (instance.Action != null)
                    yield return instance.Action;
            }
        }

        public void FastForward(IEventContext context, CancellationToken cancellationToken) { }

        public async UniTask InnerExecution(IEventContext context, CancellationToken cancellation)
        {
            var choicesThin = Choices.Select(x => new Data(x.Prerequisite?.TestPrerequisite(context) ?? true, x.Text.Content)).ToArray();
            if (context.GetDialogUI() is {} ui == false)
            {
                Debug.LogWarning($"{nameof(ScreenplayGraph.DialogUIPrefab)} has not been set, no interface to present this {nameof(Choice)} on");
                for (int i = 0; i < choicesThin.Length; i++)
                {
                    if (choicesThin[i].Enabled == false)
                        continue;

                    await Choices[i].Action.Execute(context, cancellation);
                    return;
                }

                return;
            }

            var choice = await ui.ChoicePresentation(choicesThin, cancellation);

            int index = Array.IndexOf(choicesThin, choice);
            await Choices[index].Action.Execute(context, cancellation);
            return;
        }

        public void SetupPreview(IPreviewer previewer, bool fastForwarded)
        {
            if (fastForwarded == false)
                previewer.PlaySafeAction(this);
        }

        public IEnumerable<LocalizableText> GetTextInstances()
        {
            foreach (var instance in Choices)
            {
                yield return instance.Text;
            }
        }

        public override void CollectReferences(ReferenceCollector references) { }

        [Serializable]
        public struct ChoiceInstance
        {
            [FormerlySerializedAs("Requirement")] [Input(Stroke = NoodleStroke.Dashed), SerializeReference, LabelWidth(20), HorizontalGroup(width:90), Tooltip("Which nodes need to be visited for this choice to become selectable")]
            public IPrerequisite? Prerequisite;

            [Output, SerializeReference, LabelWidth(10), HorizontalGroup, Tooltip("What will be executed when this choice is selected")]
            public IExe<IEventContext>? Action;

            [HideLabel, InlineProperty]
            public LocalizableText Text;
        }

        public record Data(bool Enabled, string Text)
        {
            /// <summary>
            /// Whether this choice has its prerequisite fulfilled
            /// </summary>
            public bool Enabled { get; } = Enabled;

            public string Text { get; } = Text;
        }
    }
}
