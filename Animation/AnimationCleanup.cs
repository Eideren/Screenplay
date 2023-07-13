using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Screenplay.Animation
{
    public class AnimationCleanup : MonoBehaviour
    {
        static readonly Dictionary<Animator, (PlayableGraph graph, AnimationPlayableOutput output, Dictionary<AnimationClip, AnimationClipPlayable> clips)> AllGraphs = new();
        readonly List<PlayableGraph> _graphs = new();

        void OnDestroy()
        {
            for (int i = 0; i < _graphs.Count; i++)
                _graphs[i].Destroy();
            _graphs.Clear();
        }

        public static (PlayableGraph graph, AnimationPlayableOutput output, Dictionary<AnimationClip, AnimationClipPlayable> clips) GetGraph(Animator animator)
        {
            if (!AllGraphs.TryGetValue(animator, out (PlayableGraph, AnimationPlayableOutput, Dictionary<AnimationClip, AnimationClipPlayable>) graphData))
            {
                if (!animator.gameObject.TryGetComponent(out AnimationCleanup cleaner))
                    cleaner = animator.gameObject.AddComponent<AnimationCleanup>();
                PlayableGraph graph = PlayableGraph.Create();
                cleaner._graphs.Add(graph);
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                graphData = (graph, AnimationPlayableOutput.Create(graph, "Routine Sample", animator), new Dictionary<AnimationClip, AnimationClipPlayable>());
                AllGraphs.Add(animator, graphData);
            }

            return graphData;
        }
    }
}