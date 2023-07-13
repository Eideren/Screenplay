using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Screenplay.Commands
{
    [Serializable] public class SceneManagement : ICommand
    {
#if UNITY_EDITOR
        [SerializeField] UnityEditor.SceneAsset _scene;
#endif
        [HideInInspector, SerializeField] public string Path;
        public Operations Operation = Operations.Replace;

        public void ValidateSelf()
        {
#if UNITY_EDITOR
            Path = _scene is not null ? UnityEditor.AssetDatabase.GetAssetPath(_scene) : null;
#endif
            if (string.IsNullOrEmpty(Path))
                throw new NullReferenceException("Scene is null");
            if (SceneUtility.GetBuildIndexByScenePath(Path) == -1)
                throw new ArgumentException($"Scene '{Path}' hasn't been added to your build");
        }

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues() => this.NoSubValues();

        public string GetInspectorString() => $"Load '{Path.EmptyToNull() ?? "??"}'";

        public IEnumerable Run(Stage stage)
        {
            Operations operation = Operation;

            AsyncOperation asyncOperation = operation switch
            {
                Operations.Add => SceneManager.LoadSceneAsync(Path, LoadSceneMode.Additive),
                Operations.Replace => SceneManager.LoadSceneAsync(Path, LoadSceneMode.Single),
                Operations.Unload => SceneManager.UnloadSceneAsync(Path, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects),
                _ => throw new NotImplementedException(Operation.ToString())
            };

            while (!asyncOperation.isDone)
                yield return null;
        }

        public enum Operations
        {
            Replace,
            Add,
            Unload
        }
    }
}