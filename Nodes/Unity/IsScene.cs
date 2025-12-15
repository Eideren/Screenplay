using System;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
using YNode;

namespace Screenplay.Nodes.Unity
{
    [Serializable, NodeVisuals(Icon = "d_TerrainInspector.TerrainToolSetheightAlt On")]
    public class IsScene : AbstractScreenplayNode, IPrerequisite
    {
        [HideLabel, HorizontalGroup] public required SceneReference Scene;

        public override void CollectReferences(ReferenceCollector references) { }

        public bool TestPrerequisite(IEventContext context)
        {
            return SceneManager.GetActiveScene().path == Scene.Path;
        }
    }
}
