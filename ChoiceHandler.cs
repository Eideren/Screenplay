using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Screenplay
{
    public class ChoiceHandler : MonoBehaviour
    {
        public Button Prototype;
        public UnityEvent OnPresentChoices;
        public UnityEvent<Button> OnSelectedChoice;
        Stage _stage;

        void OnValidate()
        {
            if (Prototype == null)
                Debug.Log($"{nameof(Prototype)} is null", this);
            else if (Prototype.GetComponentInChildren<TMP_Text>() == null)
                Debug.Log($"No TextMeshPro Text component set within {nameof(Prototype)} '{Prototype}'", this);
        }

        void OnEnable()
        {
            Prototype.gameObject.SetActive(false);
            if (_stage == null)
                Stage.UIChoiceHandler += UIChoiceHandler;
        }

        void OnDisable()
        {
            if (_stage == null)
                Stage.UIChoiceHandler -= UIChoiceHandler;
        }

        void UIChoiceHandler(Stage stage, int choices, out TMP_Text[] choiceTextComp, out Task<TMP_Text> waitingForChoice)
        {
            Stage.UIChoiceHandler -= UIChoiceHandler;
            _stage = stage;
            var buttons = new Button[choices];
            var completion = new TaskCompletionSource<TMP_Text>();
            waitingForChoice = completion.Task;
            choiceTextComp = new TMP_Text[choices];
            for (int i = 0; i < choices; i++)
            {
                var button = buttons[i] = Instantiate(Prototype.gameObject).GetComponent<Button>();
                button.transform.SetParent(Prototype.transform.parent);
                button.gameObject.SetActive(true);
                var text = choiceTextComp[i] = buttons[i].GetComponentInChildren<TMP_Text>();
                button.onClick.AddListener(delegate
                {
                    _stage = null;
                    Stage.UIChoiceHandler += UIChoiceHandler;
                    completion.SetResult(text);
                    foreach (var otherButton in buttons)
                        Destroy(otherButton.gameObject);
                    OnSelectedChoice?.Invoke(button);
                });
            }
            OnPresentChoices?.Invoke();
        }
    }
}