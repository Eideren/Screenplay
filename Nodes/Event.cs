using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    [NodeVisuals(Icon = "d_CollabConflict Icon", Width = 312)]
    public class Event : AbstractScreenplayNode, IBranch
    {
        [HideInInspector, SerializeField]
        public string Name = "My Unnamed Event";

        [Output, SerializeReference, Tooltip("What would be running when this event starts")]
        public required IExecutable? Action;
        [Input, SerializeReference, Tooltip("Interaction setup for the sole purpose of triggering this event")]
        public Precondition? TriggerSource;

        [Tooltip("Can this event ever run again after having been completed"), ToggleLeft]
        public bool Repeatable;

        [Tooltip("The scene within which this event can start")]
        public SceneReference Scene;

        public override void CollectReferences(ReferenceCollector references) { }

        public IEnumerable<IBranch?> Followup()
        {
            yield return Action;
        }
    }
}
