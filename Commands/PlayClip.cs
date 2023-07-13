using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Screenplay.Commands
{
    public class PlayClip : ICommand
    {
        public Animator Animator;
        public AnimationClip Clip;
        public float Speed = 1f;

        public void ValidateSelf()
        {
            if (!Clip)
                throw new NullReferenceException("Clip");
            if (!Animator)
                throw new NullReferenceException("Animator");
        }

        public IEnumerable<(string name, IValidatable validatable)> GetSubValues() => this.NoSubValues();
        public string GetInspectorString() => $"Play '{(Clip == null ? "null" : Clip.name)}' on '{Animator}'";

        public IEnumerable Run(Stage stage)
        {
            float f2 = Speed >= 0f ? 0f : Clip.length;
            while (true)
            {
                float deltaT = Time.deltaTime * Speed;
                f2 += deltaT;
                f2 = Mathf.Clamp(f2, 0f, Clip.length);
                Clip.EvaluateArbitrarily(f2, Animator);
                if (deltaT != 0f && (f2 <= 0f || f2 >= Clip.length))
                    break;
                yield return null;
            }
        }
    }
}