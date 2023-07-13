using System;
using System.Collections;
using System.Collections.Generic;

namespace Screenplay.Commands
{
    [Serializable] public class RunCommandAsset : ICommand
    {
        public UInterface<ICommand> CommandAsset;

        public void ValidateSelf()
        {
            if (CommandAsset.Reference == null)
                throw new NullReferenceException("commandAsset");
        }

        public string GetInspectorString() => $"Execute '{CommandAsset.Reference}'";

        public IEnumerable Run(Stage stage) => CommandAsset.Reference.Run(stage);

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues()
        {
            yield return (nameof(CommandAsset), CommandAsset.Reference);
        }
    }
}