using Unity.Mathematics;

namespace Screenplay
{
    /// <summary>
    /// Working data when executing a <see cref="ScreenplayGraph"/> shared for the whole run
    /// </summary>
    public interface IEventContext : IPrerequisiteContext
    {
        FieldRegistry FieldRegistry { get; }

        Locals Locals { get; }

        ScreenplayGraph Source => Introspection.Graph;

        ScreenplayGraph.Introspection Introspection { get; }

        ref Random GetRandom();

        bool IPrerequisiteContext.Visited(IPrerequisite executable) => Introspection.Visited.Contains(executable);

        T2 Get<T, T2>() where T : ICustomField<T2> => FieldRegistry.Get<T, T2>();
    }
}
