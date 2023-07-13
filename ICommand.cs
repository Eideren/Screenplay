using System.Collections;
using SerializeReferenceUI;

namespace Screenplay
{
    /// <summary>
    /// A function which is bound to a specific point inside of a <see cref="Scenario"/>
    /// and which will be executed through a <see cref="Stage"/> once it reaches that point
    /// </summary>
    public interface ICommand : IValidatable, IInspectorString
    {
        IEnumerable Run(Stage stage);
    }
}