using System;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Nodes
{
    [Serializable]
    public class Notes : IScreenplayNode
    {
        [SerializeField, HideInInspector] private Vector2 _position;

        [TextArea] public string Content = "";

        public Color Color = new(0.9f, 0.87f, 0.7f, 1f);

        public int Size = 3;

        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }

        public void CollectReferences(List<GenericSceneObjectReference> references) { }
    }
}
