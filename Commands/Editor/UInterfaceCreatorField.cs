using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Screenplay.Variables;
using UnityEditor;
using UnityEngine;

namespace Screenplay.Commands
{
    public class UInterfaceCreatorField
    {
        static ModalWindow modal;
        int localVersion;

        public void Draw(Rect rect, SerializedProperty fromProp, string defaultPath)
        {
            rect.SplitWithRightOf(EditorGUIUtility.singleLineHeight, out Rect fromRect, out Rect createVarRect);
            EditorGUI.PropertyField(fromRect, fromProp, GUIContent.none);
            using (new GUIBackgroundColorScope(Color.Lerp(GUI.backgroundColor, Color.green, 0.5f)))
            {
                if (GUI.Button(createVarRect, "+"))
                    OpenModalGUI(fromProp.serializedObject, fromProp.propertyPath, defaultPath);
            }

            if (localVersion != 0)
            {
                localVersion = 0;
                GUI.changed = true;
            }
        }

        void OpenModalGUI(SerializedObject obj, string propertyPath, string defaultPath)
        {
            if ((object)modal != null)
                modal.Close();
            Type[] types = AssetTypesImplementingInterface(typeof(IVariable)).ToArray();
            GUIContent[] popup = types.Select(x => new GUIContent(x.Name, $"Create a new variable of type '{x.FullName}'")).ToArray();
            int selected = 0;
            string name = "MyVariable";
            modal = ModalWindow.New(WindowTick);

            static void CreateDirectoriesToAsset(string assetPath)
            {
                if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(assetPath)))
                    RecursiveCreateDirectory(Path.GetDirectoryName(assetPath));
            }

            static void RecursiveCreateDirectory(string directory)
            {
                string parentDir = Path.GetDirectoryName(directory);
                if (string.IsNullOrEmpty(parentDir) && !AssetDatabase.IsValidFolder(parentDir))
                    RecursiveCreateDirectory(parentDir);
                AssetDatabase.CreateFolder(parentDir, Path.GetFileName(directory));
            }

            void WindowTick(EditorWindow window)
            {
                try
                {
                    window.titleContent.text = "New asset variable";
                    Rect pos = window.position;
                    pos.height = EditorGUIUtility.singleLineHeight * 3f;
                    window.position = pos;
                    window.minSize = pos.size;
                    EditorGUILayout.BeginHorizontal();
                    name = EditorGUILayout.TextField(name);
                    selected = EditorGUILayout.Popup(selected, popup);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    string path = $"Assets/{defaultPath}/{name}.asset";
                    string nonConflictingPath = AssetDatabase.GenerateUniqueAssetPath(path);
                    bool assetExistsAlready = !string.IsNullOrEmpty(nonConflictingPath) && nonConflictingPath != path;
                    if (GUILayout.Button("Cancel"))
                    {
                        window.Close();
                        modal = null;
                    }

                    if (GUILayout.Button(assetExistsAlready ? "Overwrite" : "Create"))
                    {
                        ScriptableObject variableAsset = ScriptableObject.CreateInstance(types[selected]);
                        CreateDirectoriesToAsset(path);
                        AssetDatabase.CreateAsset(variableAsset, path);
                        AssetDatabase.SaveAssets();
                        SerializedProperty property = obj.FindProperty($"{propertyPath}.UnityObj");
                        property.objectReferenceValue = variableAsset;
                        obj.ApplyModifiedProperties();
                        localVersion++;
                        window.Close();
                        modal = null;
                    }

                    EditorGUILayout.EndHorizontal();
                }
                catch (Exception)
                {
                    window.Close();
                    throw;
                }
            }
        }

        static IEnumerable<Type> AssetTypesImplementingInterface(Type desiredType)
        {
            return from x in (from asm in AppDomain.CurrentDomain.GetAssemblies() where asm.GetReferencedAssemblies().Any(asmName => AssemblyName.ReferenceMatchesDefinition(asmName, desiredType.Assembly.GetName())) select asm).Concat(new Assembly[1] { desiredType.Assembly }).SelectMany(assembly => assembly.GetTypes()) where !x.IsAbstract && desiredType.IsAssignableFrom(x) && typeof(ScriptableObject).IsAssignableFrom(x) select x;
        }
    }
}