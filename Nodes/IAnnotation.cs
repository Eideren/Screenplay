using System.Collections.Generic;

namespace Screenplay.Nodes
{
    public interface IAnnotation
    {
        void AppendTo(IEventContext context) => context.Annotations.Add(this);
    }

    public class AnnotationCollection : IAnnotation
    {
        public readonly List<IAnnotation> Annotations = new();

        public void AppendTo(IEventContext context)
        {
            foreach (var annotation in Annotations)
                annotation.AppendTo(context);
        }
    }
}
