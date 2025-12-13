using Unity.Mathematics;

namespace Screenplay
{
    /// <summary>
    /// Working data when executing a <see cref="ScreenplayGraph"/> shared for the whole run
    /// </summary>
    public interface IEventContext : IPrerequisiteContext
    {
        Locals Locals { get; }

        ScreenplayGraph Source { get; }

        /// <summary>
        /// Creates the dialog component if it doesn't exist yet and return it
        /// </summary>
        Component.UIBase? GetDialogUI();

        ref Random GetRandom();

        void Visiting(IBranch? exe);
    }
}
