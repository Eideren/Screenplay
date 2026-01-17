using System;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace Screenplay
{
    [Serializable, InlineProperty]
    public struct SceneReference : ISerializationCallbackReceiver
    {
        public string Path => _path ?? "";
        [SerializeField, HideInInspector] private string? _path;

    #if UNITY_EDITOR
        [SerializeField, HideLabel]
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

        public override bool Equals(object? obj)
        {
            if (obj is SceneReference other)
            {
#if UNITY_EDITOR
                if (other._sceneAsset == this._sceneAsset)
                    return true;
#endif
                if (other._path == _path)
                    return true;

                if (other._path.IsNullOrWhitespace() == _path.IsNullOrWhitespace())
                    return true; // Edge case, unity serializes null string as empty string, we still want those two to match between themselves

                return false;
            }

            return base.Equals(obj);
        }
    }
}
