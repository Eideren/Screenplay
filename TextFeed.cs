using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Screenplay
{
    public class TextFeed : MonoBehaviour
    {
        public TMP_Text Text;
        public UnityEvent OnOpen, OnClose, OnSwappingLine, OnTypedAllCharacters, OnTypingCharacter;
        public Stage CurrentStage;
        bool _typedAll;
        int _visibleChars;

        void OnEnable()
        {
            Stage.UIGetTextFeed += ProvideTextFeed;
        }

        void OnDisable()
        {
            Stage.UIGetTextFeed -= ProvideTextFeed;
        }

        TMP_Text ProvideTextFeed(Stage newStage)
        {
            if (CurrentStage != null && newStage == CurrentStage)
            {
                // The same stage is displaying a new line and so requests a text feed,
                // we could provide another one if we need to display multiple bubbles for different interlocutors.
                // In this case we won't, just re-use the same text feed for that new line.
                return Text;
            }

            CurrentStage = newStage;
            OnStageOpened();
            Stage.OnClose += OnCloseAny;
            return Text;

            void OnCloseAny(Stage closingStage)
            {
                if (closingStage == CurrentStage)
                {
                    OnStageClosed();
                    CurrentStage = null;
                    Stage.OnClose -= OnCloseAny;
                }
            }
        }

        void OnStageOpened()
        {
            CurrentStage.WhileOpen += TickStage;
            CurrentStage.OnDoneWithLine += OnSwappingLine.Invoke;
            OnOpen?.Invoke();
        }

        void OnStageClosed()
        {
            CurrentStage.WhileOpen -= TickStage;
            CurrentStage.OnDoneWithLine -= OnSwappingLine.Invoke;
            OnClose?.Invoke();
        }

        void TickStage()
        {
            if (_visibleChars == Text.maxVisibleCharacters)
                return;

            _visibleChars = Text.maxVisibleCharacters;

            OnTypingCharacter.Invoke();

            bool done = Stage.CountWithoutTags(Text.text, Text.text.Length) == Text.maxVisibleCharacters;
            if (done && _typedAll == false)
            {
                _typedAll = true;
                OnTypedAllCharacters?.Invoke();
            }
            else if (done == false)
            {
                _typedAll = false;
            }
        }
    }
}