using SerializeReferenceUI;

namespace Screenplay.Commands
{
    public interface IDialogTarget : IInspectorString, IValidatable
    {
        IInterlocutor GetTarget(Stage stage);
    }
}