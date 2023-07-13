using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Screenplay.Editor
{
    public class ObjectPickerDropdown : AdvancedDropdown
    {
        readonly GameObject _goScene;
        readonly Type _interfaceType;
        readonly Type[] _sceneTypes;
        readonly Type[] _scriptableObjectTypes;
        Object[] _objects;

        public ObjectPickerDropdown(Type interfaceType, Type[] sceneTypes, Type[] scriptableObjectTypes, [CanBeNull] GameObject goScene, AdvancedDropdownState state) : base(state)
        {
            _interfaceType = interfaceType;
            _sceneTypes = sceneTypes;
            _scriptableObjectTypes = scriptableObjectTypes;
            _goScene = goScene;
            Vector2 minSize = minimumSize;
            minSize.y = 300f;
            minimumSize = minSize;
        }

        public event Action<Object> OnOptionPicked;

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (item.enabled)
                OnOptionPicked?.Invoke(item.id != -1 ? _objects[item.id] : null);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new("");
            root.AddChild(new AdvancedDropdownItem("None")
            {
                id = -1
            });
            string search = string.Join(" ", _scriptableObjectTypes.Select(x => $"t:{x.Name}"));
            IEnumerable<Object> enumerable;
            if (_scriptableObjectTypes.Length != 0)
            {
                enumerable = from guid in AssetDatabase.FindAssets(search)
                    select AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).First(delegate(Object asset)
                    {
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid2, out long _);
                        return guid == guid2 && _interfaceType.IsInstanceOfType(asset);
                    });
            }
            else
            {
                IEnumerable<Object> enumerable2 = Array.Empty<Object>();
                enumerable = enumerable2;
            }

            IEnumerable<Object> assets = enumerable;
            IEnumerable<Component> enumerable3;
            if (!(_goScene == null))
                enumerable3 = _sceneTypes.SelectMany(type => _goScene.scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren(type)));
            else
            {
                IEnumerable<Component> enumerable4 = Array.Empty<Component>();
                enumerable3 = enumerable4;
            }

            IEnumerable<Component> components = enumerable3;
            _objects = components.Concat(assets).ToArray();
            int i = 0;
            Object[] objects = _objects;
            foreach (Object obj in objects)
            {
                AdvancedDropdownItem item = new(obj.ToString())
                {
                    icon = AssetPreview.GetMiniThumbnail(obj),
                    id = i++
                };
                root.AddChild(item);
            }

            return root;
        }
    }
}