using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using Object = UnityEngine.Object;

namespace Screenplay.Component
{
    public static class DestroyManager
    {
        [NoAutoStaticsCleanup] private static readonly Dictionary<Object, CancelableCompletionSource<bool>> s_onDestroyCompletion = new();
        [NoAutoStaticsCleanup] private static readonly List<Object> s_monitored = new();
        [NoAutoStaticsCleanup] private static readonly List<ScreenplayReference> s_srMonitored = new();

        public static async UniTask WaitForDestroy(Object obj, Cancellation cancellation)
        {
            if (obj == null)
                return;

            CancelableCompletionSource<bool> completion;
            lock (s_onDestroyCompletion)
            {
                if (s_onDestroyCompletion.TryGetValue(obj, out completion) == false)
                {
                    s_onDestroyCompletion[obj] = completion = new();
                    s_monitored.Add(obj);
                }
            }

            await completion.AwaitResult(cancellation);
        }

        public static void RegisterScreenplayReference(ScreenplayReference sr)
        {
            lock (s_srMonitored)
                s_srMonitored.Add(sr);
        }

        private static void CustomUpdate()
        {
            CleanupDestroyed();

            lock (s_srMonitored)
            {
                for (int i = s_srMonitored.Count - 1; i >= 0; i--)
                {
                    var reference = s_srMonitored[i];
                    s_srMonitored.RemoveAt(i);
                    if (reference != null) // Might be destroyed
                    {
                        reference.Setup();
                    }
                }
            }
        }

        private static void CleanupDestroyed()
        {
            lock (s_onDestroyCompletion)
            {
                for (int i = s_monitored.Count - 1; i >= 0; i--)
                {
                    var source = s_monitored[i];
                    if (source == null && s_onDestroyCompletion.Remove(source!, out var completion))
                    {
                        s_monitored.RemoveAt(i);
                        completion.SetResult(true);
                    }
                }
            }
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        private static void OnLoad()
        {
            // Retrieve the default Player loop system. Get the current loop instead if the default was already modified previously.
            var defaultLoop = PlayerLoop.GetDefaultPlayerLoop();

            // Create a custom update system
            var myCustomUpdate = new PlayerLoopSystem
            {
                subSystemList = null,
                updateDelegate = CustomUpdate,
                type = typeof(DestroyManager)
            };

            // Add the custom update system after the PreLateUpdate phase in the Player Loop
            var loopWithCustomUpdate = InsertSystemAfter<PreUpdate>(in defaultLoop, myCustomUpdate);
            PlayerLoop.SetPlayerLoop(loopWithCustomUpdate);
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnEnterPlaymodeInEditor;
            #endif
        }

#if UNITY_EDITOR
        private static void OnEnterPlaymodeInEditor(UnityEditor.PlayModeStateChange change)
        {
            switch (change)
            {
                case UnityEditor.PlayModeStateChange.ExitingPlayMode:
                case UnityEditor.PlayModeStateChange.ExitingEditMode:
                    CleanupDestroyed();
                    break;
                case UnityEditor.PlayModeStateChange.EnteredEditMode:
                case UnityEditor.PlayModeStateChange.EnteredPlayMode:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(change), change, null);
            }
        }
#endif

        private static PlayerLoopSystem InsertSystemAfter<T>(in PlayerLoopSystem loopSystem, PlayerLoopSystem newSystem) where T : struct
        {
            // Create a new root PlayerLoopSystem
            PlayerLoopSystem newPlayerLoop = new()
            {
                loopConditionFunction = loopSystem.loopConditionFunction,
                type = loopSystem.type,
                updateDelegate = loopSystem.updateDelegate,
                updateFunction = loopSystem.updateFunction
            };
            // Create a new list to populate with subsystems, including the custom system
            List<PlayerLoopSystem> newSubSystemList = new();

            //Iterate through the subsystems in the existing loop we passed in and add them to the new list
            if (loopSystem.subSystemList != null)
            {
                for (var i = 0; i < loopSystem.subSystemList.Length; i++)
                {
                    newSubSystemList.Add(loopSystem.subSystemList[i]);
                    // If the previously added subsystem is of the type to add after, add the custom system
                    if (loopSystem.subSystemList[i].type == typeof(T))
                    {
                        newSubSystemList.Add(newSystem);
                    }
                }
            }

            newPlayerLoop.subSystemList = newSubSystemList.ToArray();
            return newPlayerLoop;
        }
    }
}
