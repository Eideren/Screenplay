using System.Collections;
using Screech;

namespace Screenplay.Commands
{
    public interface IShowOnCondition : ICommand, IShowWhen
    {
        bool Show(Stage stage, Node line);
        bool IShowWhen.Show(object context, Node line) => Show((Stage)context, line);
        IEnumerable ICommand.Run(Stage stage)
        {
            yield break;
        }
    }
}