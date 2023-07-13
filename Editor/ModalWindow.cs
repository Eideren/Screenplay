using System;
using UnityEditor;

namespace Screenplay
{
    public class ModalWindow : EditorWindow
    {
        Action<ModalWindow> onGui;

        void OnGUI()
        {
            onGui(this);
        }

        public static ModalWindow New(Action<ModalWindow> onGui)
        {
            ModalWindow instance = CreateInstance<ModalWindow>();
            EditorApplication.update += Update;
            return instance;
            void Update()
            {
                EditorApplication.update -= Update;
                instance.onGui = onGui;
                instance.ShowModal();
            }
        }
    }
}