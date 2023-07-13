using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Screenplay
{
    /// <summary>
    /// A text asset with a simple syntax to write branching stories, they are played through a <see cref="Stage"/>.
    /// </summary>
    /// <remarks>
    /// Logic is implemented through <see cref="ICommand"/>, those are stored in the meta file of the asset
    /// </remarks>
    [Serializable] public class Scenario : ScriptableObject, IValidatable, ISerializationCallbackReceiver
    {
        public static Regex CommandPattern = new("{((\\S| )+?)}", RegexOptions.Compiled);

        public string Content = "In the beginning the Universe was created.\r\nThis had made many people very angry and has been widely regarded as a <i>bad move</i>.";
        public Binding[] Bindings = Array.Empty<Binding>();
        [HideInInspector, SerializeField] public int Version;

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
#if UNITY_EDITOR
            Version++;
            UnityEditor.EditorApplication.update += ValidationPostDeserialize;

            void ValidationPostDeserialize()
            {
                UnityEditor.EditorApplication.update -= ValidationPostDeserialize;
                try
                {
                    this.ValidateAll();
                }
                catch (ValidationException e)
                {
                    Debug.LogWarning(e.Message, e.ClosestObj ?? this);
                }
            }
#endif
        }

        public IEnumerable<(string, IValidatable)> GetSubValues()
        {
            Binding[] bindings = Bindings;
            foreach (Binding binding in bindings)
            {
                if (binding.IsSceneBound == false)
                    yield return (binding.Name, binding.Command);
            }
        }

        public void ValidateSelf()
        {
            for (int i = 0; i < Bindings.Length; i++)
            {
                if (Bindings[i].Command == null && !Bindings[i].IsSceneBound)
                    throw new InvalidOperationException($"{nameof(Bindings)} #{i} '{Bindings[i].Name}' is null");
            }
        }

        /// <summary>
        /// Transforms the text and bindings into a default c# <see cref="FormattableString"/>
        /// for parsing with <see cref="Screech.Script"/>.
        /// </summary>
        /// <remarks>
        /// Temporary, should probably use <see cref="Screech.CompoundString"/> instead.
        /// </remarks>
        /// <param name="verifySceneBindings">
        /// Whether we should throw when no overrides were provided for a scene bound binding
        /// </param>
        /// <param name="overrides">
        /// Replaces bindings of the same name by this one, used for scene bindings
        /// </param>
        /// <exception cref="ValidationException">
        /// When <paramref name="verifySceneBindings"/> is true and no matching overrides were provided
        /// </exception>
        public FormattableString AsFormattedString(bool verifySceneBindings, Span<BindingOverride> overrides)
        {
            Binding[] finalBindings = GetFinalBindings(overrides);
            var bindingsIndex = new Dictionary<string, int>();
            for (int j = 0; j < finalBindings.Length; j++)
            {
                string key = finalBindings[j].Name;
                if (!bindingsIndex.ContainsKey(key))
                    bindingsIndex.Add(finalBindings[j].Name, j);
                else
                    Debug.LogWarning($"Binding for {key} exists multiple times in this collection, using the first occurrence");
            }

            if (verifySceneBindings)
            {
                for (int i = 0; i < finalBindings.Length; i++)
                {
                    if (finalBindings[i].IsSceneBound)
                        throw new ValidationException($"Missing bindings override for '{finalBindings[i].Name}' on ScenarioInScene", this);
                }
            }

            string formatToIndexedRefs = CommandPattern.Replace(Content, delegate(Match m)
            {
                string value = m.Groups[1].Value;
                if (bindingsIndex.TryGetValue(value, out int value2))
                    return $"{{{value2}}}";
                if (verifySceneBindings)
                    throw new ValidationException($"Missing bindings for '{value}'", this);
                return "";
            });

            object[] args = finalBindings.Select((Func<Binding, object>)(x => x.Command)).ToArray();
            return FormattableStringFactory.Create(formatToIndexedRefs, args);
        }

        /// <summary>
        /// Get all interlocutors part of this <see cref="Scenario"/>
        /// </summary>
        public HashSet<IInterlocutor> GetStaticInterlocutors(Span<BindingOverride> overrides)
        {
            Binding[] finalBindings = GetFinalBindings(overrides);
            var interlocutors = new HashSet<IInterlocutor>();
            foreach (Binding binding in finalBindings)
            {
                if (binding.Command is IInterlocutorSpecifier spec)
                    interlocutors.Add(spec.GetInterlocutor());

                foreach (IValidatable allSubValue in binding.Command.GetAllSubValues()) // We have to do this nonsense for CommandAsset and Batch
                    if (allSubValue is IInterlocutorSpecifier spec2)
                        interlocutors.Add(spec2.GetInterlocutor());
            }

            return interlocutors;
        }

        Binding[] GetFinalBindings(Span<BindingOverride> overrides)
        {
            var finalBindings = new Binding[Bindings.Length];
            Array.Copy(Bindings, finalBindings, Bindings.Length);
            foreach (BindingOverride bindOverride in overrides)
            {
                for (int i = 0; i < finalBindings.Length; i++)
                {
                    if (finalBindings[i].Name == bindOverride.Name)
                    {
                        finalBindings[i].Command = bindOverride.Command;
                        finalBindings[i].IsSceneBound = false;
                    }
                }
            }

            return finalBindings;
        }

        /// <summary>
        /// A named command bound to a point inside of a <see cref="Scenario"/>.
        /// When a <see cref="Stage"/> reaches a point in the <see cref="Scenario"/>
        /// which contains this marker, it will run the associated command.
        /// </summary>
        [Serializable] public struct Binding
        {
            public string Name;

            /// <summary>
            /// Whether this command should be implemented in a scene instead of in the ScriptableObject.
            /// Implementing a command inside a scene gives you the ability to modify objects in the scene through the scenario.
            /// Without this, command would only be able to affect other assets.
            /// </summary>
            public bool IsSceneBound;

            /// <summary>
            /// The command to execute, see <see cref="Binding"/>
            /// </summary>
            [SerializeReference, SerializeReferenceButton]
            public ICommand Command;
        }
    }
}