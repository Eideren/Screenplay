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
    [NodeVisuals(60, 60, 60, Icon = "BlendTree Icon")]
    public class Choice : AbstractScreenplayNode, IExecutable, ILocalizableNode
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

        public IEnumerable<IExecutable?> Followup()
        {
            foreach (var instance in Choices)
            {
                yield return instance.Action;
            }
        }

        public UniTask Persistence(IEventContext context, Cancellation cancellation) => UniTask.CompletedTask;

        public async UniTask<IExecutable?> Execute(IEventContext context, Cancellation cancellation)
        {
            var choicesThin = Choices.Select(x => new Data(x.Prerequisite?.TestPrerequisite(context) ?? true, x.Text.Content)).ToArray();
            var ui = context.GetDialogUI();

            var choice = await ui.ChoicePresentation(choicesThin, cancellation);

            int index = Array.IndexOf(choicesThin, choice);
            return Choices[index].Action;
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
            public IExecutable? Action;

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
