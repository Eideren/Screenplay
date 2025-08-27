using Screenplay.Nodes;

namespace Screenplay
{
    /// <summary>
    /// Working data when executing a <see cref="ScreenplayGraph"/> shared for the whole run
    /// </summary>
    public interface IEventContext : IExecutableContext<IEventContext>, IPrerequisiteContext
    {
        ScreenplayGraph Source { get; }

        /// <summary>
        /// Creates the dialog component if it doesn't exist yet and return it
        /// </summary>
        Component.UIBase? GetDialogUI();

        void Visiting(IBranch? exe);

        void IExecutableContext<IEventContext>.Visiting(IExe<IEventContext>? executable) => Visiting(executable);
    }
}
