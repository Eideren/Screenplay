using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using YNode;

namespace Screenplay.Nodes
{
    public class Event : AbstractScreenplayNode, IBranch
    {
        [HideInInspector, SerializeField]
        public string Name = "My Unnamed Event";

        [Output, SerializeReference, Tooltip("What would be running when this event starts"), Required]
        public IExe<IEventContext>? Action;
        [Tooltip("Can this event ever run again after having been completed")]
        public bool Repeatable;
        [Input(Stroke = NoodleStroke.Dashed), SerializeReference, Tooltip("Which nodes need to be visited for this event to become executable")]
        public IPrerequisite? Prerequisite;
        [Input, SerializeReference, Tooltip("Interaction setup for the sole purpose of triggering this event")]
        public ITriggerSetup? TriggerSource;

        public override void CollectReferences(List<GenericSceneObjectReference> references) { }
        public IEnumerable<IBranch?> Followup()
        {
            yield return Action;
        }
    }
}
