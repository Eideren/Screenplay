using System.Threading.Tasks;
using TMPro;

namespace Screenplay
{
    public delegate void UIHandlerForChoice(Stage stage, int choices, out TMP_Text[] choiceTextComp, out Task<TMP_Text> waitingForChoice);
}