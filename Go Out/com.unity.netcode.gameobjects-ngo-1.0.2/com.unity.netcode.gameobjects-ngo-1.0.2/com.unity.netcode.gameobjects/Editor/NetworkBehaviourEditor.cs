using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Unity.Netcode.Editor
{
    [CustomEditor(typeof(NetworkBehaviour), true)]
    [CanEditMultipleObjects]
    public class NetworkBehaviourEditor : UnityEditor.Editor
    {
        private bool m_Initialized;
        private readonly List<string> m_NetworkVariableNames = new List<string>();
        private readonly Dictionary<string, FieldInfo> m_NetworkVariableFields = new Dictionary<string, FieldInfo>();
        private readonly Dictionary<string, object> m_NetworkVariableObjects = new Dictionary<string, object>();

        private GUIContent m_NetworkVariableLabelGuiContent;

        private void Init(MonoScript script)
        {
            m_Initialized = true;

            m_NetworkVariableNames.Clear();
            m_NetworkVariableFields.Clear();
            m_NetworkVariableObjects.Clear();

            m_NetworkVariableLabelGuiContent = new GUIContent("NetworkVariable", "This variable is a NetworkVariable. It can not be serialized and can only be changed during runtime.");

            var fields = script.GetClass().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            for (int i = 0; i < fields.Length; i++)
            {
                var ft = fields[i].FieldType;
                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(NetworkVariable<>) && !fields[i].IsDefined(typeof(HideInInspector), true))
                {
                    m_NetworkVariableNames.Add(fields[i].Name);
                    m_NetworkVariableFields.Add(fields[i].Name, fields[i]);
                }
            }
        }

        private void RenderNetworkVariable(int index)
        {
            if (!m_NetworkVariableFields.ContainsKey(m_NetworkVariableNames[index]))
            {
                serializedObject.Update();
                var scriptProperty = serializedObject.FindProperty("m_Script");
                if (scriptProperty == null)
                {
                    return;
                }

                var targetScript = scriptProperty.objectReferenceValue as MonoScript;
                Init(targetScript);
            }

            object value = m_NetworkVariableFields[m_NetworkVariableNames[index]].GetValue(target);
            if (value == null)
            {
                var fieldType = m_NetworkVariableFields[m_NetworkVariableNames[index]].FieldType;
                var networkVariable = (NetworkVariableBase)Activator.CreateInstance(fieldType, true);
                m_NetworkVariableFields[m_NetworkVariableNames[index]].SetValue(target, networkVariable);
            }

            var type = m_NetworkVariableFields[m_NetworkVariableNames[index]].GetValue(target).GetType();
            var genericType = type.GetGenericArguments()[0];

            EditorGUILayout.BeginHorizontal();
            if (genericType.IsValueType)
            {
                var method = typeof(NetworkBehaviourEditor).GetMethod("RenderNetworkVariableValueType", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
                var genericMethod = method.MakeGenericMethod(genericType);
                genericMethod.Invoke(this, new[] { (object)index });
            }
            else
            {
                EditorGUILayout.LabelField("Type not renderable");
            }

            GUILayout.Label(m_NetworkVariableLabelGuiContent, EditorStyles.miniLabel, GUILayout.Width(EditorStyles.miniLabel.CalcSize(m_NetworkVariableLabelGuiContent).x));
            EditorGUILayout.EndHorizontal();
        }

        private void RenderNetworkVariableValueType<T>(int index) where T : unmanaged
        {
            var networkVariable = (NetworkVariable<T>)m_NetworkVariableFields[m_NetworkVariableNames[index]].GetValue(target);
            var type = typeof(T);
            object val = networkVariable.Value;
            string name = m_NetworkVariableNames[index];

            var behaviour = (NetworkBehaviour)target;

            // Only server can MODIFY. So allow modification if network is either not running or we are server
            if (behaviour.IsBehaviourEditable())
            {
                if (type == typeof(int))
                {
                    val = EditorGUILayout.IntField(name, (int)val);
                }
                else if (type == typeof(uint))
                {
                    val = (uint)EditorGUILayout.LongField(name, (long)((uint)val));
                }
                else if (type == typeof(short))
                {
                    val = (short)EditorGUILayout.IntField(name, (int)((short)val));
                }
                else if (type == typeof(ushort))
                {
                    val = (ushort)EditorGUILayout.IntField(name, (int)((ushort)val));
                }
                else if (type == typeof(sbyte))
                {
                    val = (sbyte)EditorGUILayout.IntField(name, (int)((sbyte)val));
                }
                else if (type == typeof(byte))
                {
                    val = (byte)EditorGUILayout.IntField(name, (int)((byte)val));
                }
                else if (type == typeof(long))
                {
                    val = EditorGUILayout.LongField(name, (long)val);
                }
                else if (type == typeof(ulong))
                {
                    val = (ulong)EditorGUILayout.LongField(name, (long)((ulong)val));
                }
                else if (type == typeof(bool))
                {
                    val = EditorGUILayout.Toggle(name, (bool)val);
                }
                else if (type == typeof(string))
                {
                    val = EditorGUILayout.TextField(name, (string)val);
                }
                else if (type.IsEnum)
                {
                    val = EditorGUILayout.EnumPopup(name, (Enum)val);
                }
                else
                {
                    EditorGUILayout.LabelField("Type not renderable");
                }

                networkVariable.Value = (T)val;
            }
            else
            {
                EditorGUILayout.LabelField(name, EditorStyles.wordWrappedLabel);
                EditorGUILayout.SelectableLabel(val.ToString(), EditorStyles.wordWrappedLabel);
            }
        }


        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            if (!m_Initialized)
            {
                serializedObject.Update();
                var scriptProperty = serializedObject.FindProperty("m_Script");
                if (scriptProperty == null)
                {
                    return;
                }

                var targetScript = scriptProperty.objectReferenceValue as MonoScript;
                Init(targetScript);
            }

            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            for (int i = 0; i < m_NetworkVariableNames.Count; i++)
            {
                RenderNetworkVariable(i);
            }

            var property = serializedObject.GetIterator();
            bool expanded = true;
            while (property.NextVisible(expanded))
            {
                if (m_NetworkVariableNames.Contains(property.name))
                {
                    // Skip rendering of NetworkVars, they have special rendering
                    continue;
                }

                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (property.name == "m_Script")
                    {
                        EditorGUI.BeginDisabledGroup(true);
                    }

                    EditorGUILayout.PropertyField(property, true);

                    if (property.name == "m_Script")
                    {
                        EditorGUI.EndDisabledGroup();
                    }
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(property, true);
                    EditorGUILayout.EndHorizontal();
                }

                expanded = false;
            }

            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }

        /// <summary>
        /// Invoked once when a NetworkBehaviour component is
        /// displayed in the inspector view.
        /// </summary>
        private void OnEnable()
        {
            // This can be null and throw an exception when running test runner in the editor
            if (target == null)
            {
                return;
            }
            // When we first add a NetworkBehaviour this editor will be enabled
            // so we go ahead and check for an already existing NetworkObject here
            CheckForNetworkObject((target as NetworkBehaviour).gameObject);
        }

        internal const string AutoAddNetworkObjectIfNoneExists = "AutoAdd-NetworkObject-When-None-Exist";

        /// <summary>
        /// Recursively finds the root parent of a <see cref="Transform"/>
        /// </summary>
        /// <param name="transform">The current <see cref="Transform"/> we are inspecting for a parent</param>
        /// <returns>the root parent for the first <see cref="Transform"/> passed into the method</returns>
        public static Transform GetRootParentTransform(Transform transform)
        {
            if (transform.parent == null || transform.parent == transform)
            {
                return transform;
            }
            return GetRootParentTransform(transform.parent);
        }

        /// <summary>
        /// Used to determine if a GameObject has one or more NetworkBehaviours but
        /// does not already have a NetworkObject component.  If not it will notify
        /// the user that NetworkBehaviours require a NetworkObject.
        /// </summary>
        /// <param name="gameObject"><see cref="GameObject"/> to start checking for a <see cref="NetworkObject"/></param>
        /// <param name="networkObjectRemoved">used internally</param>
        public static void CheckForNetworkObject(GameObject gameObject, bool networkObjectRemoved = false)
        {
            // If there are no NetworkBehaviours or no gameObject, then exit early
            if (gameObject == null || (gameObject.GetComponent<NetworkBehaviour>() == null && gameObject.GetComponentInChildren<NetworkBehaviour>() == null))
            {
                return;
            }

            // Now get the root parent transform to the current GameObject (or itself)
            var rootTransform = GetRootParentTransform(gameObject.transform);
            var networkManager = rootTransform.GetComponent<NetworkManager>();
            if (networkManager == null)
            {
                networkManager = rootTransform.GetComponentInChildren<NetworkManager>();
            }

            // If there is a NetworkManager, then notify the user that a NetworkManager cannot have NetworkBehaviour components
            if (networkManager != null)
            {
                var networkBehaviours = networkManager.gameObject.GetComponents<NetworkBehaviour>();
                var networkBehavioursChildren = networkManager.gameObject.GetComponentsInChildren<NetworkBehaviour>();
                if (networkBehaviours.Length > 0 || networkBehavioursChildren.Length > 0)
                {
                    if (EditorUtility.DisplayDialog("NetworkBehaviour or NetworkManager Cannot Be Added", $"{nameof(NetworkManager)}s cannot have {nameof(NetworkBehaviour)} components added to the root parent or any of its children." +
                        $" Would you like to remove the NetworkManager or NetworkBehaviour?", "NetworkManager", "NetworkBehaviour"))
                    {
                        DestroyImmediate(networkManager);
                    }
                    else
                    {
                        foreach (var networkBehaviour in networkBehaviours)
                        {
                            DestroyImmediate(networkBehaviour);
                        }

                        foreach (var networkBehaviour in networkBehaviours)
                        {
                            DestroyImmediate(networkBehaviour);
                        }
                    }
                    return;
                }
            }

            // Otherwise, check to see if there is any NetworkObject from the root GameObject down to all children.
            // If not, notify the user that NetworkBehaviours require that the relative GameObject has a NetworkObject component.
            var networkObject = rootTransform.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                networkObject = rootTransform.GetComponentInChildren<NetworkObject>();

                if (networkObject == null)
                {
                    // If we are removing a NetworkObject but there is still one or more NetworkBehaviour components
                    // and the user has already turned "Auto-Add NetworkObject" on when first notified about the requirement
                    // then just send a reminder to the user why the NetworkObject they just deleted seemingly "re-appeared"
                    // again.
                    if (networkObjectRemoved && EditorPrefs.HasKey(AutoAddNetworkObjectIfNoneExists) && EditorPrefs.GetBool(AutoAddNetworkObjectIfNoneExists))
                    {
                        Debug.LogWarning($"{gameObject.name} still has {nameof(NetworkBehaviour)}s and Auto-Add NetworkObjects is enabled. A NetworkObject is being added back to {gameObject.name}.");
                        Debug.Log($"To reset Auto-Add NetworkObjects: Select the Netcode->General->Reset Auto-Add NetworkObject menu item.");
                    }

                    // Notify and provide the option to add it one time, always add a NetworkObject, or do nothing and let the user manually add it
                    if (EditorUtility.DisplayDialog($"{nameof(NetworkBehaviour)}s require a {nameof(NetworkObject)}",
                    $"{gameObject.name} does not have a {nameof(NetworkObject)} component.  Would you like to add one now?", "Yes", "No (manually add it)",
                    DialogOptOutDecisionType.ForThisMachine, AutoAddNetworkObjectIfNoneExists))
                    {
                        gameObject.AddComponent<NetworkObject>();
                        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
                    }
                }
            }
        }

        /// <summary>
        /// This allows users to reset the Auto-Add NetworkObject preference
        /// so the next time they add a NetworkBehaviour to a GameObject without
        /// a NetworkObject it will display the dialog box again and not
        /// automatically add a NetworkObject.
        /// </summary>
        [MenuItem("Netcode/General/Reset Auto-Add NetworkObject", false, 1)]
        private static void ResetMultiplayerToolsTipStatus()
        {
            if (EditorPrefs.HasKey(AutoAddNetworkObjectIfNoneExists))
            {
                EditorPrefs.SetBool(AutoAddNetworkObjectIfNoneExists, false);
            }
        }
    }
}
