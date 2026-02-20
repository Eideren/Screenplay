using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    [Serializable, NodeVisuals(Icon = "Git")]
    public class Bifurcate : AbstractScreenplayNode, IBifurcate
    {
        /// <summary>
        /// The different executables which will run at the same time when this node is reached
        /// </summary>
        [ListDrawerSettings(ShowFoldout = false)]
        public ExecutableEntry[] Entries = new ExecutableEntry[2];

        public IEnumerable<IExecutable?> Followup()
        {
            foreach (var executable in Entries)
                yield return executable.Executable;
        }

        public override void CollectReferences(ReferenceCollector references) { }

        [Serializable]
        public struct ExecutableEntry
        {
            [Output, SerializeReference]
            public required IExecutable Executable;
        }

        class Workaround : IExecutable
        {
            public required ExecutableEntry[] Entries;

            public Vector2 Position
            {
                get => throw new InvalidOperationException();
                set => throw new InvalidOperationException();
            }

            public void CollectReferences(ReferenceCollector references) => throw new InvalidOperationException();

            public void SetupPreview(IPreviewer previewer, bool fastForwarded) => throw new InvalidOperationException();

            public IEnumerable<IExecutable?> Followup() => throw new InvalidOperationException();

            public UniTask<IExecutable?> Execute(IEventContext context, CancellationToken cancellation)
            {
                var doneSignal = new UniTaskCompletionSource<IExecutable?>();
                int leftToDo = Entries.Length;

                foreach (var entries in Entries)
                    ParallelTask(entries.Executable).Forget();

                return doneSignal.Task.WithInterruptingCancellation(cancellation);

                async UniTask ParallelTask(IExecutable executable)
                {
                    try
                    {
                        IExecutable? i = executable;
                        do
                        {
                            var newI = await i.Execute(context, cancellation);
                            i.Persistence(context, cancellation).Forget();
                            i = newI;
                        } while (i != null);
                    }
                    finally
                    {
                        if (Interlocked.Decrement(ref leftToDo) == 0)
                            doneSignal.TrySetResult(null);
                    }
                }
            }

            public UniTask Persistence(IEventContext context, CancellationToken cancellation) => throw new NotImplementedException();
        }
    }

    public interface IBifurcate : IExecutable
    {
        UniTask<IExecutable?> IExecutable.Execute(IEventContext context, CancellationToken cancellation) => throw new NotImplementedException();
        UniTask IExecutable.Persistence(IEventContext context, CancellationToken cancellation) => throw new NotImplementedException();
        void IPreviewable.SetupPreview(IPreviewer previewer, bool fastForwarded) => throw new NotImplementedException();
    }
}
