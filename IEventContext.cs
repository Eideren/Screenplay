using System.Collections.Generic;
using Screenplay.Nodes;

namespace Screenplay
{
    /// <summary>
    /// Working data when executing a <see cref="ScreenplayGraph"/> shared for the whole run
    /// </summary>
    public interface IEventContext : IExecutableContext<IEventContext>, IPrerequisiteContext
    {
        List<IAnnotation> Annotations { get; }

        ScreenplayGraph Source { get; }

        /// <summary>
        /// Creates the dialog component if it doesn't exist yet and return it
        /// </summary>
        Component.UIBase? GetDialogUI();

        /// <summary> Return a new random seed to be used during the execution of the Screenplay </summary>
        uint NextSeed();

        void Visiting(IBranch? exe);

        void IExecutableContext<IEventContext>.Visiting(IExe<IEventContext>? executable) => Visiting(executable);

        T? TryGetFirstContextOf<T>() where T : IAnnotation
        {
            foreach (var context in Annotations)
            {
                if (context is T t)
                    return t;
            }

            return default;
        }
    }
}
