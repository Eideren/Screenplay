using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Screenplay.Component
{
    public class ScreenplayReference : MonoBehaviour, ISerializationCallbackReceiver
    {
        private static readonly Dictionary<guid, CancelableCompletionSource<Object>> s_idToRef = new(){ { default, null! } };
        private static readonly Dictionary<Object, guid> s_existingRefToId = new();

        [OnValueChanged(nameof(ReAssignReference)), SerializeField]
        private Object? Reference;

        [ReadOnly, SerializeField, DisplayAsString]
        private guid _guid = guid.New();

        public guid Guid => _guid;

        public void OnBeforeSerialize() {}

        public void OnAfterDeserialize()
        {
            CancelableCompletionSource<Object> completion;
            lock (s_idToRef)
            {
                if (s_idToRef.TryGetValue(Guid, out completion))
                {
                    if (completion.IsCompleted(out var existingRef))
                    {
                        Debug.LogError($"Id conflict between {existingRef.GetInstanceID()} and {Reference?.GetInstanceID()}");
                        return;
                    }

                    // Use existing completion
                }
                else
                {
                    s_idToRef[Guid] = completion = new CancelableCompletionSource<Object>();
                }

                if (Reference is null)
                {
                    Debug.LogError($"{nameof(Reference)} is null");
                    return;
                }

                s_existingRefToId[Reference] = Guid;
            }

            DestroyManager.RegisterScreenplayReference(this);
        }

        public void Setup()
        {
            if (Reference != null)
            {
                if (s_idToRef.TryGetValue(Guid, out var completion))
                {
                    if (completion.IsCompleted(out _) == false)
                        completion.SetResult(Reference);
                }
            }

            MonitorLifetime().Forget();

            async UniTask MonitorLifetime()
            {
                await DestroyManager.WaitForDestroy(Reference!, Cancellation.None);

                OnDestroyProxy();
            }
        }

        private void ReAssignReference()
        {
            CancelableCompletionSource<Object> completion;
            lock (s_idToRef)
            {
                if (s_idToRef.TryGetValue(Guid, out completion))
                {
                    if (completion.IsCompleted(out var previousRef))
                    {
                        s_existingRefToId.Remove(previousRef);
                        s_idToRef[Guid] = completion = new CancelableCompletionSource<Object>();
                    }

                    // completion is waiting for a result, set its result
                }
                else
                {
                    s_idToRef[Guid] = completion = new CancelableCompletionSource<Object>();
                }

                if (Reference == null)
                    return;

                s_existingRefToId[Reference] = Guid;
            }

            completion.SetResult(Reference);
        }

        private void OnDestroy()
        {
            OnDestroyProxy();
        }

        private void OnDestroyProxy()
        {
            lock (s_idToRef)
            {
                if (s_idToRef.TryGetValue(Guid, out var completion)
                    && completion.IsCompleted(out var existingRef)
                    && ReferenceEquals(existingRef, Reference))
                {
                    s_idToRef.Remove(Guid);
                    s_existingRefToId.Remove(Reference);
                }
            }
        }

        public static bool TryGetRef(guid guid, [NotNullWhen(true)] out Object? obj)
        {
            lock (s_idToRef)
            {
                obj = null;
                return guid != default
                       && s_idToRef.TryGetValue(guid, out var completion)
                       && completion.IsCompleted(out obj)
                       // obj may be destroyed
                       && obj != null
                       && (obj is not MonoBehaviour mb || mb.destroyCancellationToken.IsCancellationRequested == false);
            }
        }

        public static bool TryGetId(Object obj, out guid guid)
        {
            lock (s_idToRef)
                return s_existingRefToId.TryGetValue(obj, out guid);
        }

        public static async UniTask<T> GetAsync<T>(guid guid, Cancellation cancellation) where T : Object
        {
            while (true)
            {
                CancelableCompletionSource<Object> completion;
                lock (s_idToRef)
                {
                    if (s_idToRef.TryGetValue(guid, out completion) == false)
                        s_idToRef[guid] = completion = new CancelableCompletionSource<Object>();
                }

                if (completion.IsCompleted(out var output) == false)
                    output = await completion.AwaitResult(cancellation);

                if (output == null || output is MonoBehaviour mono && mono.destroyCancellationToken.IsCancellationRequested) // This is for cases where the monobehavior is inside its OnDestroy scope
                {
                    lock (s_idToRef)
                    {
                        if (s_idToRef.TryGetValue(guid, out var newCompletion))
                        {
                            if (completion != newCompletion)
                                continue; // idToRef changed under us, wait for this new one instead

                            s_idToRef.Remove(guid);
                            s_existingRefToId.Remove(output!);
                        }
                    }
                }
                else
                {
                    return (T)output;
                }
            }
        }

        public static guid GetOrCreate(GameObject obj)
        {
            lock (s_idToRef)
            {
                if (s_existingRefToId.TryGetValue(obj, out var guid))
                {
                    return guid;
                }

                var sRef = obj.AddComponent<ScreenplayReference>();
                sRef.Reference = obj;
                guid = sRef.Guid;
                sRef.ReAssignReference();

                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(obj);
                #endif
                return guid;
            }
        }

        public static guid GetOrCreate(UnityEngine.Component comp)
        {
            lock (s_idToRef)
            {
                if (s_existingRefToId.TryGetValue(comp, out var guid))
                {
                    return guid;
                }

                var sRef = comp.gameObject.AddComponent<ScreenplayReference>();
                sRef.Reference = comp;
                guid = sRef.Guid;
                sRef.ReAssignReference();

                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(comp);
                #endif
                return guid;
            }
        }
    }
}
