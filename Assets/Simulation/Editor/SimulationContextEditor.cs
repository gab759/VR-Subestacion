using UnityEditor;
using UnityEngine;
using VRSubestacion.Simulation;
using VRSubestacion.Simulation.Integration;

namespace VRSubestacion.EditorTools
{
    [CustomEditor(typeof(SimulationContext))]
    public sealed class SimulationContextEditor : UnityEditor.Editor
    {
        private WiringReport _lastReport;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var context = (SimulationContext)target;

            EditorGUILayout.Space();

            if (Application.isPlaying && context.Binder != null)
            {
                SimulationBinder binder = context.Binder;
                EditorGUILayout.LabelField("Estado", binder.Lifecycle.ToString());
                EditorGUILayout.LabelField("Paso", $"{binder.Director.CurrentStepIndex}/{binder.Director.StepCount}");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validar cableado"))
                {
                    _lastReport = SimulationWiringValidator.ValidateAll();
                    _lastReport.LogToConsole("SimulationWiring");
                }

                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    if (GUILayout.Button("Auto-asignar canales"))
                        AutoAssignChannels();
                }
            }

            if (_lastReport == null)
                return;

            for (int i = 0; i < _lastReport.Issues.Count; i++)
            {
                WiringIssue issue = _lastReport.Issues[i];
                EditorGUILayout.HelpBox(issue.Message, ToMessageType(issue.Severity));
            }
        }

        private void AutoAssignChannels()
        {
            var input = SimulationWiringValidator.FindSingleAsset<InteractionSignalChannel>();
            var progress = SimulationWiringValidator.FindSingleAsset<ProcedureProgressChannel>();

            serializedObject.Update();

            SerializedProperty inputProp = serializedObject.FindProperty("inputChannel");
            SerializedProperty progressProp = serializedObject.FindProperty("progressChannel");

            if (inputProp.objectReferenceValue == null && input != null)
                inputProp.objectReferenceValue = input;
            if (progressProp.objectReferenceValue == null && progress != null)
                progressProp.objectReferenceValue = progress;

            serializedObject.ApplyModifiedProperties();

            if (input == null || progress == null)
                Debug.LogWarning("[SimulationWiring] No se encontró un único asset de cada canal. Usa 'Create Missing Channel Assets' o asigna manualmente.", target);
        }

        private static MessageType ToMessageType(WiringSeverity severity)
        {
            switch (severity)
            {
                case WiringSeverity.Error:
                    return MessageType.Error;
                case WiringSeverity.Warning:
                    return MessageType.Warning;
                default:
                    return MessageType.Info;
            }
        }
    }
}
