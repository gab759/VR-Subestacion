using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRSubestacion.Simulation;
using VRSubestacion.Simulation.Integration;

#if VRSUB_META_ISDK
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using VRSubestacion.Adapters.MetaXR;
#endif

namespace VRSubestacion.EditorTools
{
    /// <summary>
    /// Añade y cablea los adaptadores del switch principal y del botón rojo
    /// sobre los objetos legacy de la escena activa.
    /// </summary>
    public static class SetupInteractablesTool
    {
        private const string MenuPath = "VR Subestacion/Setup/Setup Initial Interactables (Switch & Button)";
        private const string UndoName = "Setup Initial Interactables";
        private const string RootName = "_SimulationRoot";
        private const string SwitchStepOn = "BajarSwitchPrincipal";
        private const string ButtonStepOff = "ApagarSubestacion_BotonRojo";

        [MenuItem(MenuPath, priority = 1)]
        public static void SetupInitialInteractables()
        {
#if !VRSUB_META_ISDK
            EditorUtility.DisplayDialog(
                "VR Subestacion",
                "No se puede configurar los interactables: falta el Meta Interaction SDK (com.meta.xr.sdk.interaction).",
                "OK");
#else
            var channel = AssetDatabase.LoadAssetAtPath<InteractionSignalChannel>(
                SimulationWiringValidator.InputChannelAssetPath);

            if (channel == null)
            {
                EditorUtility.DisplayDialog(
                    "VR Subestacion",
                    "No se encontró Assets/Simulation/Channels/InteractionSignalChannel.asset.\n" +
                    "Crea el canal con 'VR Subestacion/Validation/Create Missing Channel Assets' e inténtalo de nuevo.",
                    "OK");
                Debug.LogError("[SetupInteractables] Falta InteractionSignalChannel.asset.");
                return;
            }

            GameObject switchGo = FindSwitchPrincipal();
            if (switchGo == null)
            {
                EditorUtility.DisplayDialog(
                    "VR Subestacion",
                    "No se encontró el Switch Principal (SlidingDoor con EvaluateStep(\"BajarSwitchPrincipal\")).",
                    "OK");
                Debug.LogError("[SetupInteractables] Switch Principal no encontrado.");
                return;
            }

            GameObject buttonGo = FindRedButton();
            if (buttonGo == null)
            {
                EditorUtility.DisplayDialog(
                    "VR Subestacion",
                    "No se encontró el Botón Rojo (PushButton con EvaluateStep(\"ApagarSubestacion_BotonRojo\")).",
                    "OK");
                Debug.LogError("[SetupInteractables] Botón Rojo no encontrado.");
                return;
            }

            ShadowModeBridge bridge = FindShadowModeBridge();
            if (bridge == null)
            {
                EditorUtility.DisplayDialog(
                    "VR Subestacion",
                    "No se encontró ShadowModeBridge en '_SimulationRoot'.\n" +
                    "Ejecuta primero 'VR Subestacion/Setup/Auto Wire Scene'.",
                    "OK");
                Debug.LogError("[SetupInteractables] Falta _SimulationRoot / ShadowModeBridge.");
                return;
            }

            Undo.SetCurrentGroupName(UndoName);
            int undoGroup = Undo.GetCurrentGroup();

            LeverRotationAdapter lever = ConfigureSwitch(switchGo, channel);
            PokeInteractionAdapter poke = ConfigureRedButton(buttonGo, channel);
            RegisterCleanBehaviours(bridge, lever, poke);

            EditorUtility.SetDirty(switchGo);
            EditorUtility.SetDirty(buttonGo);
            EditorUtility.SetDirty(bridge);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog(
                "VR Subestacion",
                "Los interactables (Switch Principal y Botón Rojo) fueron configurados con éxito.",
                "OK");
#endif
        }

        [MenuItem(MenuPath, true)]
        private static bool SetupInitialInteractablesValidate()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode
                && SceneManager.GetActiveScene().IsValid();
        }

#if VRSUB_META_ISDK
        private static LeverRotationAdapter ConfigureSwitch(GameObject go, InteractionSignalChannel channel)
        {
            EnsureCollider(go);
            Rigidbody body = EnsureKinematicBody(go);

            OneGrabRotateTransformer transformer = go.GetComponent<OneGrabRotateTransformer>();
            if (transformer == null)
                transformer = Undo.AddComponent<OneGrabRotateTransformer>(go);

            using (var transformerSo = new SerializedObject(transformer))
            {
                transformerSo.FindProperty("_rotationAxis").enumValueIndex = (int)OneGrabRotateTransformer.Axis.Right;
                transformerSo.ApplyModifiedProperties();
            }

            Grabbable grabbable = go.GetComponent<Grabbable>();
            if (grabbable == null)
                grabbable = Undo.AddComponent<Grabbable>(go);

            grabbable.InjectOptionalOneGrabTransformer(transformer);
            grabbable.InjectOptionalRigidbody(body);
            grabbable.InjectOptionalThrowWhenUnselected(false);
            grabbable.MaxGrabPoints = 1;

            GrabInteractable grabInteractable = go.GetComponent<GrabInteractable>();
            if (grabInteractable == null)
                grabInteractable = Undo.AddComponent<GrabInteractable>(go);

            grabInteractable.InjectRigidbody(body);
            grabInteractable.InjectOptionalPointableElement(grabbable);

            LeverRotationAdapter adapter = go.GetComponent<LeverRotationAdapter>();
            if (adapter == null)
                adapter = Undo.AddComponent<LeverRotationAdapter>(go);

            Transform measured = ReadObjectReference(go, "SlidingDoor", "pivot") as Transform;

            using (var so = new SerializedObject(adapter))
            {
                so.FindProperty("grabPointable").objectReferenceValue = grabbable;
                so.FindProperty("measuredTransform").objectReferenceValue = measured != null ? measured : go.transform;
                so.FindProperty("localAxis").vector3Value = Vector3.right;
                ConfigureSignalOutput(so.FindProperty("onSwitchedOn"), channel, ProcedureSignal.BajarSwitchPrincipal);
                ConfigureSignalOutput(so.FindProperty("onSwitchedOff"), channel, ProcedureSignal.SubirSwitchPrincipal);
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(adapter);
            EditorUtility.SetDirty(grabbable);
            EditorUtility.SetDirty(grabInteractable);
            return adapter;
        }

        private static PokeInteractionAdapter ConfigureRedButton(GameObject go, InteractionSignalChannel channel)
        {
            Collider collider = EnsureCollider(go);

            PlaneSurface plane = go.GetComponent<PlaneSurface>();
            if (plane == null)
                plane = Undo.AddComponent<PlaneSurface>(go);

            CircleSurface circle = go.GetComponent<CircleSurface>();
            if (circle == null)
                circle = Undo.AddComponent<CircleSurface>(go);

            using (var circleSo = new SerializedObject(circle))
            {
                circleSo.FindProperty("_planeSurface").objectReferenceValue = plane;
                circleSo.FindProperty("_radius").floatValue = EstimatePokeRadius(collider);
                circleSo.ApplyModifiedProperties();
            }

            PokeInteractable pokeInteractable = go.GetComponent<PokeInteractable>();
            if (pokeInteractable == null)
                pokeInteractable = Undo.AddComponent<PokeInteractable>(go);

            pokeInteractable.InjectSurfacePatch(circle);

            PokeInteractionAdapter adapter = go.GetComponent<PokeInteractionAdapter>();
            if (adapter == null)
                adapter = Undo.AddComponent<PokeInteractionAdapter>(go);

            using (var so = new SerializedObject(adapter))
            {
                so.FindProperty("pointable").objectReferenceValue = pokeInteractable;
                ConfigureSignalOutput(
                    so.FindProperty("onPressed"),
                    channel,
                    ProcedureSignal.ApagarSubestacion_BotonRojo,
                    ProcedureSignal.EncenderSubestacion_BotonRojo);
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(adapter);
            EditorUtility.SetDirty(pokeInteractable);
            EditorUtility.SetDirty(circle);
            return adapter;
        }

        private static void RegisterCleanBehaviours(ShadowModeBridge bridge, params Behaviour[] behaviours)
        {
            using (var so = new SerializedObject(bridge))
            {
                SerializedProperty clean = so.FindProperty("cleanBehaviours");
                for (int i = 0; i < behaviours.Length; i++)
                    AddBehaviourIfMissing(clean, behaviours[i]);
                so.ApplyModifiedProperties();
            }
        }

        private static Collider EnsureCollider(GameObject go)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider == null)
                collider = Undo.AddComponent<BoxCollider>(go);
            return collider;
        }

        private static Rigidbody EnsureKinematicBody(GameObject go)
        {
            Rigidbody body = go.GetComponent<Rigidbody>();
            if (body == null)
                body = Undo.AddComponent<Rigidbody>(go);

            body.useGravity = false;
            body.isKinematic = true;
            return body;
        }

        private static float EstimatePokeRadius(Collider collider)
        {
            if (collider == null)
                return 0.05f;

            Vector3 extents = collider.bounds.extents;
            return Mathf.Max(0.01f, Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z)));
        }

        private static void ConfigureSignalOutput(
            SerializedProperty output,
            InteractionSignalChannel channel,
            params ProcedureSignal[] signals)
        {
            output.FindPropertyRelative("channel").objectReferenceValue = channel;

            SerializedProperty signalsProp = output.FindPropertyRelative("signals");
            signalsProp.arraySize = signals.Length;
            for (int i = 0; i < signals.Length; i++)
                signalsProp.GetArrayElementAtIndex(i).intValue = (int)signals[i];
        }

        private static GameObject FindSwitchPrincipal()
        {
            GameObject byEvent = FindByEvaluateStep("SlidingDoor", new[] { "onOpen", "onClose" }, SwitchStepOn);
            if (byEvent != null)
                return byEvent;

            return FindSceneObject("pCube31")
                ?? FindSceneObjectContaining("SwitchPrincipal")
                ?? FindSceneObjectContaining("Switch Principal");
        }

        private static GameObject FindRedButton()
        {
            GameObject byEvent = FindByEvaluateStep("PushButton", new[] { "onPressed" }, ButtonStepOff);
            if (byEvent != null)
                return byEvent;

            return FindBySerializedString("PushButton", new[] { "hintStepName", "allowedSteps" }, ButtonStepOff);
        }

        private static ShadowModeBridge FindShadowModeBridge()
        {
            GameObject root = FindSceneObject(RootName);
            if (root != null)
            {
                ShadowModeBridge onRoot = root.GetComponent<ShadowModeBridge>();
                if (onRoot != null)
                    return onRoot;
            }

            ShadowModeBridge[] bridges = UnityEngine.Object.FindObjectsByType<ShadowModeBridge>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            return bridges.Length > 0 ? bridges[0] : null;
        }

        private static GameObject FindByEvaluateStep(string typeName, string[] eventProperties, string stepName)
        {
            Type type = Type.GetType(typeName + ", Assembly-CSharp");
            if (type == null)
                return null;

            UnityEngine.Object[] found = UnityEngine.Object.FindObjectsByType(
                type, FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < found.Length; i++)
            {
                var component = found[i] as Component;
                if (component == null)
                    continue;

                using (var so = new SerializedObject(component))
                {
                    for (int e = 0; e < eventProperties.Length; e++)
                    {
                        if (HasEvaluateStep(so.FindProperty(eventProperties[e]), stepName))
                            return component.gameObject;
                    }
                }
            }

            return null;
        }

        private static bool HasEvaluateStep(SerializedProperty eventProp, string stepName)
        {
            if (eventProp == null)
                return false;

            SerializedProperty calls = eventProp.FindPropertyRelative("m_PersistentCalls.m_Calls");
            if (calls == null)
                return false;

            for (int i = 0; i < calls.arraySize; i++)
            {
                SerializedProperty call = calls.GetArrayElementAtIndex(i);
                string method = call.FindPropertyRelative("m_MethodName").stringValue;
                string argument = call.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue;
                if (method == "EvaluateStep" && argument == stepName)
                    return true;
            }

            return false;
        }

        private static GameObject FindBySerializedString(string typeName, string[] propertyNames, string value)
        {
            Type type = Type.GetType(typeName + ", Assembly-CSharp");
            if (type == null)
                return null;

            UnityEngine.Object[] found = UnityEngine.Object.FindObjectsByType(
                type, FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < found.Length; i++)
            {
                var component = found[i] as Component;
                if (component == null)
                    continue;

                using (var so = new SerializedObject(component))
                {
                    for (int p = 0; p < propertyNames.Length; p++)
                    {
                        if (SerializedPropertyContains(so.FindProperty(propertyNames[p]), value))
                            return component.gameObject;
                    }
                }
            }

            return null;
        }

        private static bool SerializedPropertyContains(SerializedProperty property, string value)
        {
            if (property == null)
                return false;

            if (property.propertyType == SerializedPropertyType.String)
                return property.stringValue == value;

            if (!property.isArray)
                return false;

            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                if (element.propertyType == SerializedPropertyType.String && element.stringValue == value)
                    return true;
            }

            return false;
        }

        private static UnityEngine.Object ReadObjectReference(GameObject go, string typeName, string propertyName)
        {
            Type type = Type.GetType(typeName + ", Assembly-CSharp");
            if (type == null)
                return null;

            Component component = go.GetComponent(type);
            if (component == null)
                return null;

            using (var so = new SerializedObject(component))
            {
                SerializedProperty property = so.FindProperty(propertyName);
                return property != null ? property.objectReferenceValue : null;
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

        private static GameObject FindSceneObjectContaining(string fragment)
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return transforms[i].gameObject;
            }

            return null;
        }
#endif
    }
}
