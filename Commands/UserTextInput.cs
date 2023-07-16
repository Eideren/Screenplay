using System;
using System.Collections;
using System.Collections.Generic;
using Screenplay.Variables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Screenplay.Commands
{
    [Serializable] public class UserTextInput : ICommand
    {
        public UInterface<IVariable> Variable;
        public string PlaceholderText = "Enter text...";
        public string ButtonText = "Accept";
        public int MinimumLength = 1;

        public void ValidateSelf()
        {
            if (Variable.Reference == null)
                throw new NullReferenceException(nameof(Variable));
        }

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues()
        {
            yield return (nameof(Variable), Variable.Reference);
        }

        public string GetInspectorString() => $"Set {(Variable.Reference == null ? "null" : Variable.Reference)} to what the user typed";
        public IEnumerable Run(Stage stage)
        {
            const float bg_color = 41f / 255f;
            const float deco_color = 34f / 255f;

            var canvasGO = new GameObject(nameof(UserTextInput));
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<GraphicRaycaster>();

            var root = new GameObject("InputField");
            root.transform.parent = canvas.transform;

            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new(0.4f, 0.48f);
            rootRect.anchorMax = new(0.6f, 0.52f);
            //rootRect.sizeDelta = Vector2.zero;
            rootRect.offsetMin = new(0, 0);
            rootRect.offsetMax = new(0, 0);

            var textArea = CreateUIObject("Text Area", root);
            var childPlaceholder = CreateUIObject("Placeholder", textArea);
            var childText = CreateUIObject("Text", textArea);

            var image = root.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>("UI/Skin/InputFieldBackground.psd");
            image.type = Image.Type.Sliced;
            image.color = new Color(bg_color, bg_color, bg_color);

            var inputField = root.AddComponent<TMP_InputField>();

            var textAreaRectTransform = textArea.GetComponent<RectTransform>();
            textAreaRectTransform.anchorMin = Vector2.zero;
            textAreaRectTransform.anchorMax = Vector2.one;
            textAreaRectTransform.sizeDelta = Vector2.zero;
            textAreaRectTransform.offsetMin = new Vector2(10, 6);
            textAreaRectTransform.offsetMax = new Vector2(-10, -7);

            // This nonsense is to work around a TextMeshPro issue where the caret is 3 times taller when there isn't any text
            RectMask2D rectMask = textArea.AddComponent<RectMask2D>();
            #if UNITY_2019_4_OR_NEWER && !UNITY_2019_4_1 && !UNITY_2019_4_2 && !UNITY_2019_4_3 && !UNITY_2019_4_4 && !UNITY_2019_4_5 && !UNITY_2019_4_6 && !UNITY_2019_4_7 && !UNITY_2019_4_8 && !UNITY_2019_4_9 && !UNITY_2019_4_10 && !UNITY_2019_4_11
            rectMask.padding = new Vector4(-8, -5, -8, -5);
            #endif

            var text = childText.AddComponent<TextMeshProUGUI>();
            text.text = "";
            text.enableAutoSizing = true;
            text.enableWordWrapping = false;
            text.extraPadding = true;
            text.richText = true;
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;

            var placeholder = childPlaceholder.AddComponent<TextMeshProUGUI>();
            placeholder.text = PlaceholderText;
            placeholder.enableAutoSizing = true;
            placeholder.fontSize = 14;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.alignment = TextAlignmentOptions.Center;
            placeholder.enableWordWrapping = false;
            placeholder.extraPadding = true;

            // Make placeholder color half as opaque as normal text color.
            Color placeholderColor = text.color;
            placeholderColor.a *= 0.5f;
            placeholder.color = placeholderColor;

            // Add Layout component to placeholder.
            placeholder.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var textRectTransform = childText.GetComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.sizeDelta = Vector2.zero;
            textRectTransform.offsetMin = new(0, 0);
            textRectTransform.offsetMax = new(0, 0);

            var placeholderRectTransform = childPlaceholder.GetComponent<RectTransform>();
            placeholderRectTransform.anchorMin = Vector2.zero;
            placeholderRectTransform.anchorMax = Vector2.one;
            placeholderRectTransform.sizeDelta = Vector2.zero;
            placeholderRectTransform.offsetMin = new(0, 0);
            placeholderRectTransform.offsetMax = new(0, 0);

            inputField.textViewport = textAreaRectTransform;
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.fontAsset = text.font;

            var buttonGO = new GameObject("Button");
            buttonGO.transform.SetParent(canvas.transform, false);

            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new(0.4f, 0.44f);
            buttonRect.anchorMax = new(0.6f, 0.48f);
            buttonRect.offsetMin = new(0, 0);
            buttonRect.offsetMax = new(0, 0);

            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(bg_color, bg_color, bg_color);

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            var buttonText = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            buttonText.text = ButtonText;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.transform.SetParent(canvas.transform, false);
            buttonText.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;
            buttonText.raycastTarget = false; // blocks buttons otherwise

            var buttonTextRect = (RectTransform)buttonText.transform;
            buttonTextRect.SetParent(button.transform, false);
            buttonTextRect.anchorMin = new(0f, 0f);
            buttonTextRect.anchorMax = new(1f, 1f);
            buttonTextRect.offsetMin = new(0, 0);
            buttonTextRect.offsetMax = new(0, 0);


            bool clicked = false;
            button.onClick.AddListener(delegate
            {
                clicked = true;
            });

            inputField.enabled = false; // Not sure why but I have to disable and re-enable input field for the caret to work
            inputField.enabled = true;
            string currentString = inputField.text;
            button.interactable = false;
            while (clicked == false || button.interactable == false)
            {
                if (text.text != currentString)
                {
                    currentString = inputField.text;
                    button.interactable = IsInputValid(currentString);
                }

                yield return null;
            }

            Variable.Reference.SetFromParsedString(currentString);
            GameObject.Destroy(canvasGO);



            static GameObject CreateUIObject(string name, GameObject parent)
            {
                GameObject go = new GameObject(name);
                go.AddComponent<RectTransform>();
                go.transform.SetParent(parent.transform, false);
                return go;
            }

        }

        bool IsInputValid(string input)
        {
            return Variable.Reference.CanParse(input) && input.Length >= MinimumLength;
        }
    }
}