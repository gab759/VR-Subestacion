using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRSubestacion.Simulation;
using VRSubestacion.Simulation.Integration;

namespace VRSubestacion.EditorTools
{
    /// <summary>
    /// Instala en la escena abierta el cableado mínimo de Shadow Mode:
    /// _SimulationRoot + ShadowModeBridge + SimulationContext y los canales del proyecto.
    /// </summary>
    public static class SceneAutoWiringTool
    {
        private const string MenuPath = "VR Subestacion/Setup/Auto Wire Scene";
        private const string RootName = "_SimulationRoot";
        private const string ContextName = "SimulationContext";
        private const string ChecklistObjectName = "Substation_ChecklistManager";
        private const string ProcedureObjectName = "Substation_ProcedureManager";
        private const string ChecklistTypeName = "CheckList";
        private const string ProcedureTypeName = "SubstationProcedureManager";
        private const string UndoName = "Auto Wire Scene";

        [MenuItem(MenuPath, priority = 0)]
        public static void AutoWireScene()
        {
            Undo.SetCurrentGroupName(UndoName);
            int undoGroup = Undo.GetCurrentGroup();

            GameObject root = FindOrCreateSimulationRoot();
            ShadowModeBridge bridge = EnsureShadowModeBridge(root);
            SimulationContext context = FindOrCreateSimulationContext(root);
            AssignChannels(context);
            WireBridge(bridge, context);

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(bridge);
            EditorUtility.SetDirty(context);
            EditorSceneManager.MarkSceneDirty(root.scene);

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog(
                "VR Subestacion",
                "El cableado de Shadow Mode se completó exitosamente.",
                "OK");
        }

        [MenuItem(MenuPath, true)]
        private static bool AutoWireSceneValidate()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode
                && SceneManager.GetActiveScene().IsValid();
        }

        private static GameObject FindOrCreateSimulationRoot()
        {
            GameObject root = FindSceneObject(RootName);
            if (root != null)
                return root;

            root = new GameObject(RootName);
            root.transform.position = Vector3.zero;
            SceneManager.MoveGameObjectToScene(root, SceneManager.GetActiveScene());
            Undo.RegisterCreatedObjectUndo(root, UndoName);
            return root;
        }

        private static ShadowModeBridge EnsureShadowModeBridge(GameObject root)
        {
            ShadowModeBridge bridge = root.GetComponent<ShadowModeBridge>();
            if (bridge == null)
                bridge = Undo.AddComponent<ShadowModeBridge>(root);

            using (var so = new SerializedObject(bridge))
            {
                so.FindProperty("mode").enumValueIndex = (int)ArchitectureMode.Shadow;
                so.ApplyModifiedProperties();
            }

            return bridge;
        }

        private static SimulationContext FindOrCreateSimulationContext(GameObject root)
        {
            Transform child = root.transform.Find(ContextName);
            GameObject contextGo;

            if (child == null)
            {
                contextGo = new GameObject(ContextName);
                contextGo.transform.SetParent(root.transform, false);
                contextGo.transform.localPosition = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(contextGo, UndoName);
            }
            else
            {
                contextGo = child.gameObject;
            }

            SimulationContext context = contextGo.GetComponent<SimulationContext>();
            if (context == null)
                context = Undo.AddComponent<SimulationContext>(contextGo);

            return context;
        }

        private static void AssignChannels(SimulationContext context)
        {
            var input = AssetDatabase.LoadAssetAtPath<InteractionSignalChannel>(
                SimulationWiringValidator.InputChannelAssetPath);
            var progress = AssetDatabase.LoadAssetAtPath<ProcedureProgressChannel>(
                SimulationWiringValidator.ProgressChannelAssetPath);

            using (var so = new SerializedObject(context))
            {
                so.FindProperty("inputChannel").objectReferenceValue = input;
                so.FindProperty("progressChannel").objectReferenceValue = progress;
                so.ApplyModifiedProperties();
            }
        }

        private static void WireBridge(ShadowModeBridge bridge, SimulationContext context)
        {
            Behaviour checklist = FindLegacyBehaviour(ChecklistObjectName, ChecklistTypeName);
            Behaviour procedure = FindLegacyBehaviour(ProcedureObjectName, ProcedureTypeName);

            using (var so = new SerializedObject(bridge))
            {
                so.FindProperty("mode").enumValueIndex = (int)ArchitectureMode.Shadow;
                AddBehaviourIfMissing(so.FindProperty("legacyBehaviours"), checklist);
                AddBehaviourIfMissing(so.FindProperty("legacyBehaviours"), procedure);
                AddBehaviourIfMissing(so.FindProperty("cleanBehaviours"), context);
                so.ApplyModifiedProperties();
            }
        }

        private static void AddBehaviourIfMissing(SerializedProperty arrayProp, Behaviour behaviour)
        {
            if (behaviour == null)
                return;

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                if (arrayProp.GetArrayElementAtIndex(i).objectReferenceValue == behaviour)
                    return;
            }

            int index = arrayProp.arraySize;
            arrayProp.arraySize++;
            arrayProp.GetArrayElementAtIndex(index).objectReferenceValue = behaviour;
        }

        private static Behaviour FindLegacyBehaviour(string objectName, string typeName)
        {
            Type type = Type.GetType(typeName + ", Assembly-CSharp");
            GameObject go = FindSceneObject(objectName);

            if (go != null)
            {
                if (type != null)
                {
                    var typed = go.GetComponent(type) as Behaviour;
                    if (typed != null)
                        return typed;
                }

                Behaviour[] behaviours = go.GetComponents<Behaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] != null && behaviours[i].GetType().Name == typeName)
                        return behaviours[i];
                }
            }

            if (type == null)
                return null;

            UnityEngine.Object[] found = UnityEngine.Object.FindObjectsByType(
                type, FindObjectsInactive.Include, FindObjectsSortMode.None);
            return found.Length > 0 ? found[0] as Behaviour : null;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == objectName)
                    return transforms[i].gameObject;
            }

            return null;
        }
    }
}
