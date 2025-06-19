using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Screenplay
{
    [Serializable, InlineProperty]
    public struct SceneReference : ISerializationCallbackReceiver
    {
        public string Path => _path ?? "";
        [SerializeField, HideInInspector] private string? _path;

    #if UNITY_EDITOR
        [SerializeField, ValidateInput(nameof(SceneNotNull)), HideLabel]
        private UnityEditor.SceneAsset _sceneAsset;

        public SceneReference(UnityEditor.SceneAsset asset)
        {
            _sceneAsset = asset;
            _path = UnityEditor.AssetDatabase.GetAssetPath(asset);
        }

        private bool SceneNotNull(UnityEditor.SceneAsset asset, ref string message)
        {
            if (asset == null)
            {
                _path = null;
                message = "Scene must not be null";
                return false;
            }
            _path = UnityEditor.AssetDatabase.GetAssetPath(asset);
            bool found = false;
            foreach (var scene in UnityEditor.EditorBuildSettings.scenes)
                if (_path == scene.path)
                    found = true;

            if (found == false)
            {
                message = $"Scene {asset.name} is not part of the build";
                return false;
            }

            return true;
        }

        private bool SceneNotPartOfBuild()
        {
            if (_sceneAsset == null)
                return false;

            _path = UnityEditor.AssetDatabase.GetAssetPath(_sceneAsset);
            foreach (var scene in UnityEditor.EditorBuildSettings.scenes)
                if (_path == scene.path)
                    return false;

            return true;
        }

        [ShowIf(nameof(SceneNotPartOfBuild))]
        [Button]
        private void AddSceneToBuild()
        {
            UnityEditor.EditorBuildSettings.scenes = UnityEditor.EditorBuildSettings.scenes
                .Append(new UnityEditor.EditorBuildSettingsScene(_path, true))
                .ToArray();
        }
#endif

        public void OnBeforeSerialize()
        {
    #if UNITY_EDITOR
            // Make sure the path is always updated
            if (_sceneAsset != null)
                _path = UnityEditor.AssetDatabase.GetAssetPath(_sceneAsset);
    #endif
        }

        public void OnAfterDeserialize(){ }

        public bool IsValid()
        {
            return _path != null;
        }
    }
}
