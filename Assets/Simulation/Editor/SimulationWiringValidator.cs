using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRSubestacion.Presentation;
using VRSubestacion.Simulation;
using VRSubestacion.Simulation.Integration;

namespace VRSubestacion.EditorTools
{
    /// <summary>
    /// Validación de cableado en tiempo de edición. Las asignaciones de aquí son de Editor y no afectan el runtime.
    /// </summary>
    public static class SimulationWiringValidator
    {
        public const string ChannelsFolder = "Assets/Simulation/Channels";
        public const string InputChannelAssetPath = ChannelsFolder + "/InteractionSignalChannel.asset";
        public const string ProgressChannelAssetPath = ChannelsFolder + "/ProcedureProgressChannel.asset";

        public static WiringReport ValidateAll()
        {
            var report = new WiringReport();

            SimulationContext[] contexts = FindInScenes<SimulationContext>();
            ChecklistPresenter[] presenters = FindInScenes<ChecklistPresenter>();
            ShadowModeBridge[] bridges = FindInScenes<ShadowModeBridge>();

            bool sceneUsesClean = contexts.Length > 0 || presenters.Length > 0 || bridges.Length > 0;

            ValidateProject(report, sceneUsesClean);

            if (!sceneUsesClean)
            {
                report.Info("Las escenas abiertas no usan la arquitectura nueva; no hay cableado que validar.");
                return report;
            }

            ValidateScene(contexts, presenters, bridges, report, requirePersistentAssets: true);

            if (contexts.Length == 1)
            {
                ScanChannelReferences(
                    FindInScenes<MonoBehaviour>(),
                    contexts[0].InputChannel,
                    contexts[0].ProgressChannel,
                    report);
            }

            return report;
        }

        public static void ValidateProject(WiringReport report, bool missingIsError)
        {
            ValidateAssetCount<InteractionSignalChannel>(report, missingIsError);
            ValidateAssetCount<ProcedureProgressChannel>(report, missingIsError);
        }

        public static void ValidateScene(
            SimulationContext[] contexts,
            ChecklistPresenter[] presenters,
            ShadowModeBridge[] bridges,
            WiringReport report,
            bool requirePersistentAssets)
        {
            ValidateBridges(bridges, contexts, report);

            if (contexts.Length > 1)
                report.Warning($"Hay {contexts.Length} SimulationContext en escena; se esperaba uno solo.", contexts[1]);

            for (int i = 0; i < contexts.Length; i++)
                ValidateContext(contexts[i], report, requirePersistentAssets);

            for (int i = 0; i < presenters.Length; i++)
                ValidatePresenter(presenters[i], contexts, report);
        }

        public static void ValidateContext(SimulationContext context, WiringReport report, bool requirePersistentAssets)
        {
            SimulationBindingIssues issues = SimulationBinder.Validate(context.InputChannel, context.ProgressChannel);

            if ((issues & SimulationBindingIssues.MissingInputChannel) != 0)
                report.Error($"SimulationContext '{context.name}': falta InteractionSignalChannel (input).", context);
            if ((issues & SimulationBindingIssues.MissingProgressChannel) != 0)
                report.Error($"SimulationContext '{context.name}': falta ProcedureProgressChannel (progress).", context);

            if (requirePersistentAssets)
            {
                if (context.InputChannel != null && !EditorUtility.IsPersistent(context.InputChannel))
                    report.Error($"SimulationContext '{context.name}': el canal de entrada no es un asset del proyecto.", context);
                if (context.ProgressChannel != null && !EditorUtility.IsPersistent(context.ProgressChannel))
                    report.Error($"SimulationContext '{context.name}': el canal de progreso no es un asset del proyecto.", context);
            }

            ChecklistPresenter[] presenters = context.Presenters;
            if (presenters == null || presenters.Length == 0)
            {
                report.Warning($"SimulationContext '{context.name}': no tiene presenters asignados.", context);
                return;
            }

            for (int i = 0; i < presenters.Length; i++)
            {
                if (presenters[i] == null)
                    report.Warning($"SimulationContext '{context.name}': presenter vacío en el índice {i}.", context);
            }
        }

        public static void ValidatePresenter(ChecklistPresenter presenter, SimulationContext[] contexts, WiringReport report)
        {
            SimulationContext owner = FindOwner(presenter, contexts);

            if (owner == null)
            {
                if (presenter.ProgressChannel == null)
                    report.Error($"ChecklistPresenter '{presenter.name}': sin canal de progreso y ningún SimulationContext lo inyecta.", presenter);
            }
            else if (presenter.ProgressChannel != null && presenter.ProgressChannel != owner.ProgressChannel)
            {
                report.Warning($"ChecklistPresenter '{presenter.name}': su canal difiere del de '{owner.name}'; el contexto lo reemplazará al iniciar.", presenter);
            }

            int rowCount = presenter.RowCount;
            if (rowCount == 0)
            {
                report.Warning($"ChecklistPresenter '{presenter.name}': no tiene filas configuradas.", presenter);
                return;
            }

            var seen = new HashSet<ProcedureStep>();
            for (int i = 0; i < rowCount; i++)
            {
                ChecklistPresenter.Row row = presenter.GetRow(i);

                if (row.step == ProcedureStep.None)
                    report.Warning($"ChecklistPresenter '{presenter.name}': la fila {i} no tiene paso asignado.", presenter);
                else if (!seen.Add(row.step))
                    report.Warning($"ChecklistPresenter '{presenter.name}': el paso {row.step} está repetido (fila {i}).", presenter);

                if (row.view == null)
                    report.Warning($"ChecklistPresenter '{presenter.name}': la fila {i} no tiene TmpChecklistItemView.", presenter);
            }
        }

        public static void ValidateBridges(ShadowModeBridge[] bridges, SimulationContext[] contexts, WiringReport report)
        {
            if (bridges.Length > 1)
                report.Error($"Hay {bridges.Length} ShadowModeBridge en escena; solo debe existir uno.", bridges[1]);

            if (bridges.Length == 0)
            {
                if (contexts.Length > 0)
                    report.Warning("No hay ShadowModeBridge: la arquitectura nueva correrá siempre junto a la anterior.");
                return;
            }

            ShadowModeBridge bridge = bridges[0];
            ArchitectureMode mode = bridge.EffectiveMode;

            if (ArchitectureModeRules.HasCompileOverride && mode != bridge.Mode)
                report.Info($"ShadowModeBridge: el modo {bridge.Mode} del Inspector está anulado por define de compilación ({mode}).", bridge);

            if (ArchitectureModeRules.IsCleanActive(mode) && contexts.Length == 0)
                report.Error($"ShadowModeBridge en modo {mode} pero no hay SimulationContext en escena.", bridge);

            for (int i = 0; i < contexts.Length; i++)
            {
                if (!Contains(bridge.CleanBehaviours, contexts[i]))
                    report.Warning($"SimulationContext '{contexts[i].name}' no está en 'Clean Behaviours' del bridge; no se apagará en modo Legacy.", contexts[i]);
            }

            if (mode == ArchitectureMode.Clean && IsEmpty(bridge.LegacyBehaviours) && IsEmpty(bridge.LegacyObjects))
                report.Warning("ShadowModeBridge en modo Clean sin elementos legacy: el flujo anterior seguirá activo.", bridge);
        }

        /// <summary>
        /// Busca referencias serializadas a canales distintos de los del contexto (p. ej. SignalOutput de adaptadores).
        /// </summary>
        public static void ScanChannelReferences(
            MonoBehaviour[] behaviours,
            InteractionSignalChannel expectedInput,
            ProcedureProgressChannel expectedProgress,
            WiringReport report)
        {
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour is SimulationContext)
                    continue;

                using (var so = new SerializedObject(behaviour))
                {
                    SerializedProperty it = so.GetIterator();
                    while (it.Next(true))
                    {
                        if (it.propertyType != SerializedPropertyType.ObjectReference)
                            continue;

                        Object value = it.objectReferenceValue;

                        if (value is InteractionSignalChannel input && expectedInput != null && input != expectedInput)
                        {
                            report.Warning($"{behaviour.GetType().Name} '{behaviour.name}' ({it.propertyPath}) emite a '{input.name}', que el director no escucha.", behaviour);
                        }
                        else if (value is ProcedureProgressChannel progress && expectedProgress != null && progress != expectedProgress)
                        {
                            report.Warning($"{behaviour.GetType().Name} '{behaviour.name}' ({it.propertyPath}) escucha '{progress.name}', que el director no publica.", behaviour);
                        }
                    }
                }
            }
        }

        public static void CreateMissingChannelAssets()
        {
            if (!AssetDatabase.IsValidFolder(ChannelsFolder))
                AssetDatabase.CreateFolder("Assets/Simulation", "Channels");

            CreateIfMissing<InteractionSignalChannel>(InputChannelAssetPath);
            CreateIfMissing<ProcedureProgressChannel>(ProgressChannelAssetPath);

            AssetDatabase.SaveAssets();
        }

        public static T FindSingleAsset<T>() where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids.Length != 1)
                return null;
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void CreateIfMissing<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.FindAssets("t:" + typeof(T).Name).Length > 0)
                return;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[SimulationWiring] Creado {path}", asset);
        }

        private static void ValidateAssetCount<T>(WiringReport report, bool missingIsError) where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            string typeName = typeof(T).Name;

            if (guids.Length == 0)
            {
                string message = $"No existe ningún asset {typeName}. Usa 'VR Subestacion/Validation/Create Missing Channel Assets'.";
                if (missingIsError)
                    report.Error(message);
                else
                    report.Warning(message);
            }
            else if (guids.Length > 1)
            {
                report.Warning($"Hay {guids.Length} assets {typeName}; verifica que adaptadores y contexto usen el mismo.");
            }
        }

        private static SimulationContext FindOwner(ChecklistPresenter presenter, SimulationContext[] contexts)
        {
            for (int i = 0; i < contexts.Length; i++)
            {
                if (Contains(contexts[i].Presenters, presenter))
                    return contexts[i];
            }
            return null;
        }

        private static bool Contains<T>(T[] array, T item) where T : Object
        {
            if (array == null)
                return false;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == item)
                    return true;
            }
            return false;
        }

        private static bool IsEmpty<T>(T[] array) where T : Object
        {
            if (array == null)
                return true;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != null)
                    return false;
            }
            return true;
        }

        private static T[] FindInScenes<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }
    }
}
