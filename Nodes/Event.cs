using System.Collections.Generic;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    [NodeVisuals(Icon = "d_CollabConflict Icon", Width = 312)]
    public class Event : AbstractScreenplayNode, IBranch
    {
        [HideInInspector, SerializeField]
        public string Name = "My Unnamed Event";

        /// <summary>
        /// What would be running when this event starts
        /// </summary>
        [Output, SerializeReference]
        public required IExecutable? Action;
        /// <summary>
        /// Interaction setup for the sole purpose of triggering this event
        /// </summary>
        [Input, SerializeReference]
        public Precondition? TriggerSource;


        /// <summary>
        /// Can this event ever run again after having been completed
        /// </summary>
        public bool Repeatable;

        /// <summary>
        /// The scene within which this event can start
        /// </summary>
        public SceneReference Scene;

        public override void CollectReferences(ReferenceCollector references) { }

        public IEnumerable<IBranch> Followup()
        {
            yield return Action!;
        }
    }
}
