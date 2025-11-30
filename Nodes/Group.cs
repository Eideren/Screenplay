using System;
using System.Collections.Generic;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    [Serializable]
    public class Group : AbstractScreenplayNode
    {
        public string Description = "My Group Description";

        public Vector2 Size = new Vector2(100, 100);

        [SerializeReference] public List<INodeValue> Children = new();

        public override void CollectReferences(ReferenceCollector references) { }
    }
}
