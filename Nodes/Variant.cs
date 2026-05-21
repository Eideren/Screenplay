using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    [Serializable, NodeVisuals(Icon = "Git")]
    public class Variant : AbstractScreenplayNode, IExecutable
    {
        public SelectionBehavior Selection = SelectionBehavior.FirstNonVisitedOrLast;

        [Output, SerializeReference, ListDrawerSettings(ShowFoldout = false)]
        public IExecutable?[] Branches = new IExecutable?[2];

        public enum SelectionBehavior
        {
            Random,
            FirstNonVisitedOrLast,
        }

        public IEnumerable<IExecutable?> Followup()
        {
            foreach (var choice in Branches)
                yield return choice;
        }

        public UniTask<IExecutable?> Execute(IEventContext context, Cancellation cancellation)
        {
            IExecutable? output;
            if (Branches.Length != 0)
            {
                switch (Selection)
                {
                    case SelectionBehavior.Random:
                        output = Branches[context.GetRandom().NextInt(Branches.Length)];
                        break;
                    case SelectionBehavior.FirstNonVisitedOrLast:
                        output = Branches[^1];
                        foreach (var branch in Branches)
                        {
                            if (branch is not null && context.Visited(branch) == false)
                            {
                                output = branch;
                                break;
                            }
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                output = null;
            }

            return new UniTask<IExecutable?>(output);
        }

        public UniTask Persistence(IEventContext context, Cancellation cancellation) => UniTask.CompletedTask;

        public void SetupPreview(IPreviewer previewer, bool fastForwarded) { }

        public override void CollectReferences(ReferenceCollector references) { }
    }
}
