using System;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Nodes
{
    [Serializable]
    public abstract class AbstractScreenplayNode : IScreenplayNode
    {
        [SerializeField, HideInInspector] private Vector2 _position;

        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }

        public abstract void CollectReferences(List<GenericSceneObjectReference> references);
    }
}
