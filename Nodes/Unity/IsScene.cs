using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;

namespace Screenplay.Nodes.Unity
{
    [Serializable]
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
