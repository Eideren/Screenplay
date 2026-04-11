using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Screenplay.Nodes;

namespace Screenplay.Component
{
    public class DialogUIComponent : UIBase
    {
        public bool AdvancesAutomatically = false;
        [SerializeReference] public IFastForward FastForward = new InputFastForward();
        public DialogChoiceTemplate DialogChoiceTemplate = null!;
        public UnityEvent<string>? OnNewLine;
        public UnityEvent? OnLineFullyVisible;
        public UnityEvent<int>? SetHowManyCharactersAreVisible;
        public UnityEvent? OnStart, OnEnd;
        public UnityEvent? OnChoicePresented, OnChoiceClosed;

        public override bool DialogAdvancesAutomatically => AdvancesAutomatically;
        public override bool FastForwardRequested => FastForward.IsRequesting();
        public override void StartLineTypewriting(string line) => OnNewLine?.Invoke(line);
        public override void FinishedTypewriting() => OnLineFullyVisible?.Invoke();
        public override void SetTypewritingCharacter(int characters) => SetHowManyCharactersAreVisible?.Invoke(characters);
        public override void StartDialogPresentation() => OnStart?.Invoke();
        public override void EndDialogPresentation() => OnEnd?.Invoke();

        public override async UniTask<Choice.Data> ChoicePresentation(Choice.Data[] choices, Cancellation cancellation)
        {
            DialogChoiceTemplate.gameObject.SetActive(false);
            OnStart?.Invoke();
            OnChoicePresented?.Invoke();

            var tasks = new List<UniTask<Choice.Data>>();

            var choiceGameObjects = new List<GameObject>();
            foreach (var choice in choices)
            {
                if (choice.Enabled == false)
                    continue;

                var uiChoice = Instantiate(DialogChoiceTemplate, DialogChoiceTemplate.transform.parent);
                choiceGameObjects.Add(uiChoice.gameObject);
                uiChoice.gameObject.SetActive(true);
                uiChoice.Label.text = choice.Text;
                uiChoice.Button.interactable = choice.Enabled;
                tasks.Add(AwaitClick());

                async UniTask<Choice.Data> AwaitClick()
                {
                    await uiChoice.Button.onClick.OnInvokeAsync(cancellation.GetStandardToken());
                    return choice;
                }
            }

            try
            {
                return (await UniTask.WhenAny(tasks)).result;
            }
            finally
            {
                foreach (var go in choiceGameObjects)
                {
                    if (Application.isPlaying)
                        Destroy(go);
                    else
                        DestroyImmediate(go);
                }
                OnChoiceClosed?.Invoke();
                OnEnd?.Invoke();
            }
        }

        private void Awake()
        {
            DialogChoiceTemplate.gameObject.SetActive(false);
        }

        public interface IFastForward
        {
            bool IsRequesting();
        }

        [Serializable]
        public class InputFastForward : IFastForward
        {
            public InputActionReference Input = null!;

            public bool IsRequesting()
            {
                return Input.action.WasPerformedThisFrame();
            }
        }
    }
}
