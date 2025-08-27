using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Screenplay
{
    public class AnimationRollback : IAnimationRollback
    {
        private readonly Action? _data;

        public void Dispose()
        {
            Rollback();
        }

        public AnimationRollback(GameObject go, AnimationClip clip)
        {
            _data = GetObjectState(go, clip);
        }

        public AnimationRollback(Animator animator, int hash, int layerIndex)
        {
            _data = GetObjectState(animator, hash, layerIndex);
        }

        public void Rollback()
        {
            _data?.Invoke();
        }

        private static Action? GetObjectState(GameObject root, AnimationClip clip)
        {
            Action? a = null;
            foreach (var fnc in ExtractRewinds(root, clip))
                a += fnc;

            return a;
        }

        private static Action? GetObjectState(Animator animator, int hash, int layerIndex)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
            var layer = controller.layers[layerIndex];
            var state = layer.stateMachine.states.First(x => x.state.nameHash == hash);
            var motion = state.state.motion;

            Action? a = null;
            foreach (var clip in ExtractClips(motion))
            {
                foreach (var fnc in ExtractRewinds(animator.gameObject, clip))
                    a += fnc;
            }
            return a;
        }

        private static IEnumerable<Action> ExtractRewinds(GameObject root, AnimationClip clip)
        {
            foreach (var group in RetrieveBindings(clip).GroupBy(x => x.path))
            {
                var firstBinding = group.First();
                var target = AnimationUtility.GetAnimatedObject(root, firstBinding);
                if (target == null)
                {
                    Debug.LogWarning($"Could not find {firstBinding.path}");
                    continue;
                }

                Action? rollbackAction = null;
                if (target is Animator anim)
                {
                    for (HumanBodyBones i = 0; i < HumanBodyBones.LastBone; i++)
                    {
                        var transform = anim.GetBoneTransform(i);
                        if (transform)
                        {
                            var localT = transform.localPosition;
                            var localQ = transform.localRotation;
                            rollbackAction += () =>
                            {
                                transform.localPosition = localT;
                                transform.localRotation = localQ;
                            };
                        }
                    }
                    {
                        var transform = anim.transform;
                        var localT = transform.localPosition;
                        var localQ = transform.localRotation;
                        rollbackAction += () =>
                        {
                            transform.localPosition = localT;
                            transform.localRotation = localQ;
                        };
                    }
                }

                var serializedObject = new SerializedObject(target);
                foreach (var editorCurveBinding in group)
                {
                    string propName = editorCurveBinding.propertyName;

                    if (_Dof.Contains(propName))
                        continue;

                    var serializedProp = serializedObject.FindProperty(propName);
                    if (serializedProp is not null)
                    {
                        object val = serializedProp.boxedValue;
                        rollbackAction += () => { serializedProp.boxedValue = val; };
                        continue;
                    }

                    if ((propName.EndsWith(".x") || propName.EndsWith(".y") || propName.EndsWith(".z") || propName.EndsWith(".w"))
                        && propName[^3] is 'T' or 'Q')
                    {
                        if (HumanBodyBones.TryParse(propName[..^3], out HumanBodyBones v1))
                            continue;
                        if (propName[..^3] == "Root")
                            continue;
                    }

                    Debug.LogWarning($"Could not find {propName}");
                }

                if (rollbackAction is not null)
                {
                    rollbackAction += () => serializedObject.ApplyModifiedProperties();
                    yield return rollbackAction;
                }
            }
        }

        private static IEnumerable<EditorCurveBinding> RetrieveBindings(AnimationClip clip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                yield return binding;

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                yield return binding;
        }

        private static IEnumerable<AnimationClip> ExtractClips(Motion? motion)
        {
            if (motion is AnimationClip aClip)
            {
                yield return aClip;
            }
            else if (motion is BlendTree blendTree)
            {
                foreach (var childMotion in blendTree.children)
                {
                    foreach (var animationClip in ExtractClips(childMotion.motion))
                    {
                        yield return animationClip;
                    }
                }
            }
            else if (motion is not null)
            {
                throw new NotImplementedException(motion.GetType().Name);
            }
        }

        private static HashSet<string> _Dof = new()
        {
            "Spine Front-Back",
            "Spine Left-Right",
            "Spine Twist Left-Right",
            "Chest Front-Back",
            "Chest Left-Right",
            "Chest Twist Left-Right",
            "UpperChest Front-Back",
            "UpperChest Left-Right",
            "UpperChest Twist Left-Right",
            "Neck Nod Down-Up",
            "Neck Tilt Left-Right",
            "Neck Turn Left-Right",
            "Head Nod Down-Up",
            "Head Tilt Left-Right",
            "Head Turn Left-Right",
            "Left Eye Down-Up",
            "Left Eye In-Out",
            "Right Eye Down-Up",
            "Right Eye In-Out",
            "Jaw Close",
            "Jaw Left-Right",
            "Neck Nod Down-Up",
            "Neck Tilt Left-Right",
            "Neck Turn Left-Right",
            "Head Nod Down-Up",
            "Head Tilt Left-Right",
            "Head Turn Left-Right",
            "Left Eye Down-Up",
            "Left Eye In-Out",
            "Right Eye Down-Up",
            "Right Eye In-Out",
            "Jaw Close",
            "Jaw Left-Right",
            "Left Upper Leg Front-Back",
            "Left Upper Leg In-Out",
            "Left Upper Leg Twist In-Out",
            "Left Lower Leg Stretch",
            "Left Lower Leg Twist In-Out",
            "Left Foot Up-Down",
            "Left Foot Twist In-Out",
            "Left Toes Up-Down",
            "Right Upper Leg Front-Back",
            "Right Upper Leg In-Out",
            "Right Upper Leg Twist In-Out",
            "Right Lower Leg Stretch",
            "Right Lower Leg Twist In-Out",
            "Right Foot Up-Down",
            "Right Foot Twist In-Out",
            "Right Toes Up-Down",
            "Left Shoulder Down-Up",
            "Left Shoulder Front-Back",
            "Left Arm Down-Up",
            "Left Arm Front-Back",
            "Left Arm Twist In-Out",
            "Left Forearm Stretch",
            "Left Forearm Twist In-Out",
            "Left Hand Down-Up",
            "Left Hand In-Out",
            "Right Shoulder Down-Up",
            "Right Shoulder Front-Back",
            "Right Arm Down-Up",
            "Right Arm Front-Back",
            "Right Arm Twist In-Out",
            "Right Forearm Stretch",
            "Right Forearm Twist In-Out",
            "Right Hand Down-Up",
            "Right Hand In-Out",
            "LeftHand.Thumb.1 Stretched",
            "LeftHand.Thumb.Spread",
            "LeftHand.Thumb.2 Stretched",
            "LeftHand.Thumb.3 Stretched",
            "LeftHand.Index.1 Stretched",
            "LeftHand.Index.Spread",
            "LeftHand.Index.2 Stretched",
            "LeftHand.Index.3 Stretched",
            "LeftHand.Middle.1 Stretched",
            "LeftHand.Middle.Spread",
            "LeftHand.Middle.2 Stretched",
            "LeftHand.Middle.3 Stretched",
            "LeftHand.Ring.1 Stretched",
            "LeftHand.Ring.Spread",
            "LeftHand.Ring.2 Stretched",
            "LeftHand.Ring.3 Stretched",
            "LeftHand.Little.1 Stretched",
            "LeftHand.Little.Spread",
            "LeftHand.Little.2 Stretched",
            "LeftHand.Little.3 Stretched",
            "RightHand.Thumb.1 Stretched",
            "RightHand.Thumb.Spread",
            "RightHand.Thumb.2 Stretched",
            "RightHand.Thumb.3 Stretched",
            "RightHand.Index.1 Stretched",
            "RightHand.Index.Spread",
            "RightHand.Index.2 Stretched",
            "RightHand.Index.3 Stretched",
            "RightHand.Middle.1 Stretched",
            "RightHand.Middle.Spread",
            "RightHand.Middle.2 Stretched",
            "RightHand.Middle.3 Stretched",
            "RightHand.Ring.1 Stretched",
            "RightHand.Ring.Spread",
            "RightHand.Ring.2 Stretched",
            "RightHand.Ring.3 Stretched",
            "RightHand.Little.1 Stretched",
            "RightHand.Little.Spread",
            "RightHand.Little.2 Stretched",
            "RightHand.Little.3 Stretched",
        };
    }
}
