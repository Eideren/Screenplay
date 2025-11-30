using System.Linq;
using UnityEngine;

namespace Screenplay.Nodes
{
    public abstract class VariantBase : AbstractScreenplayNode
    {
        public abstract guid[] GetVariantGuids();
    }

    public abstract class VariantBase<T> : VariantBase where T : IIdentifiable
    {
        public T GetVariant(IEventContext context)
        {
            if (context.Variants.TryGetValue(this, out var val))
            {
                foreach (var variant in GetVariants())
                {
                    if (variant.Guid == val)
                    {
                        return variant;
                    }
                }

                Debug.LogError($"Failed to get variant for guid {val}, returning first one");
                return GetVariants()[0];
            }

            Debug.LogError($"Failed to get variant for {this}, returning first one");
            return GetVariants()[0];
        }

        public override guid[] GetVariantGuids() => GetVariants().Select(x => x.Guid).ToArray();
        protected abstract T[] GetVariants();
    }

    public interface IIdentifiable
    {
        public guid Guid { get; }
    }
}
