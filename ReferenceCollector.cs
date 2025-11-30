using System.Collections.Generic;
using Screenplay.Nodes;
using Object = UnityEngine.Object;

namespace Screenplay
{
    public class ReferenceCollector
    {
        public readonly List<GenericSceneObjectReference> RawData = new();

        public void Collect<T>(params SceneObjectReference<T>[] t) where T : Object
        {
            foreach (var objectReference in t)
                RawData.Add(objectReference);
        }

        public void Collect<T>(params T?[] t) where T : IScreenplayNode
        {
            foreach (var objectReference in t)
                objectReference?.CollectReferences(this);
        }

        public void Clear() => RawData.Clear();
    }
}
