using System;
using System.Collections.Generic;
using Screenplay.Animation;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Screenplay
{
    public static class AnimationExtensions
    {
        static readonly Dictionary<AnimationClip, AnimationEvent[]> _events = new();

        public static ReadOnlySpan<AnimationEvent> GetEventsNonAlloc(this AnimationClip clip)
        {
            if (!_events.TryGetValue(clip, out AnimationEvent[] events))
                _events.Add(clip, events = clip.events);
            return events;
        }

        public static string KeysToCSharpDeclaration(this AnimationCurve curve)
        {
            string s = "";
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe keyframe = keys[i];
                s += $"new Keyframe{{time = {keyframe.time}f, value = {keyframe.value}f, inTangent = {keyframe.inTangent}f, outTangent = {keyframe.outTangent}f, inWeight = {keyframe.inWeight}f, outWeight = {keyframe.outWeight}f, weightedMode = WeightedMode.{keyframe.weightedMode}}},\n";
            }

            return s;
        }

        public static void EvaluateArbitrarily(this AnimationClip clip, double time, Animator animator)
        {
            (PlayableGraph, AnimationPlayableOutput, Dictionary<AnimationClip, AnimationClipPlayable>) graphData = AnimationCleanup.GetGraph(animator);
            if (!graphData.Item3.TryGetValue(clip, out AnimationClipPlayable playableClip))
            {
                playableClip = AnimationClipPlayable.Create(graphData.Item1, clip);
                graphData.Item3.Add(clip, playableClip);
            }

            graphData.Item2.SetSourcePlayable(playableClip);
            playableClip.SetTime(time);
            graphData.Item1.Evaluate();
        }
    }
}