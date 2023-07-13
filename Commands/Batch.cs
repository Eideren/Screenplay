using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Screenplay.Commands
{
    [Serializable] public class Batch : ICommand
    {
        public enum BatchMode
        {
            Sequentially,
            Simultaneously
        }

        public BatchMode Mode;

        [SerializeReference, SerializeReferenceButton]
        public ICommand[] Commands = Array.Empty<ICommand>();

        public IEnumerable Run(Stage stage)
        {
            return Mode switch
            {
                BatchMode.Sequentially => Sequentially(stage),
                BatchMode.Simultaneously => Simultaneously(stage),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public void ValidateSelf()
        {
            if (Commands.Length == 0)
                throw new InvalidOperationException($"{nameof(Commands)} is empty");

            for (int i = 0; i < Commands.Length; i++)
            {
                if (Commands[i] == null)
                    throw new NullReferenceException($"#{i}");
            }
        }

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues()
        {
            for (int i = 0; i < Commands.Length; i++)
                yield return (i.ToString(), Commands[i]);
        }

        public string GetInspectorString()
        {
            BatchMode mode = Mode;

            string result = mode switch
            {
                BatchMode.Sequentially => $"( {string.Join(" ) then ( ", Commands.Select(x => x?.GetInspectorString()))} )",
                BatchMode.Simultaneously => $"Do ( {string.Join(" ) and ( ", Commands.Select(x => x?.GetInspectorString()))} ) simultaneously",
                _ => throw new ArgumentOutOfRangeException()
            };

            return result;
        }

        IEnumerable Sequentially(Stage stage)
        {
            ICommand[] commands = Commands;
            foreach (ICommand command in commands)
            {
                foreach (object item in command.Run(stage))
                    yield return item;
            }
        }

        IEnumerable Simultaneously(Stage stage)
        {
            IEnumerator[] enums = Commands.Select(x => x.Run(stage).GetEnumerator()).ToArray();
            int leftRunning = enums.Length;
            if (leftRunning == 0)
                yield break;

            while (true)
            {
                for (int i = 0; i < enums.Length; i++)
                {
                    if (enums[i] == null || enums[i].MoveNext())
                        continue;

                    enums[i] = null;
                    leftRunning--;
                    if (leftRunning == 0)
                        yield break;
                }

                yield return null;
            }
        }
    }
}