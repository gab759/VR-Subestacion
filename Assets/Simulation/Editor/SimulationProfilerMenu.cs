using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRSubestacion.Simulation.Integration;

namespace VRSubestacion.EditorTools
{
    public static class SimulationProfilerMenu
    {
        private const string MenuRoot = "VR Subestacion/Profiling/";
        private const string OpenProfilerMenu = MenuRoot + "Open Profiler";
        private const string DeepProfileMenu = MenuRoot + "Deep Profile (Editor)";
        private const string AddMonitorMenu = MenuRoot + "Add Allocation Monitor To Simulation Context";

        [MenuItem(OpenProfilerMenu, priority = 0)]
        private static void OpenProfiler()
        {
            EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
        }

        /// <remarks>Cambiar Deep Profile recompila el dominio; hazlo fuera de Play Mode.</remarks>
        [MenuItem(DeepProfileMenu, priority = 1)]
        private static void ToggleDeepProfile()
        {
            ProfilerDriver.deepProfiling = !ProfilerDriver.deepProfiling;
        }

        [MenuItem(DeepProfileMenu, true)]
        private static bool ToggleDeepProfileValidate()
        {
            Menu.SetChecked(DeepProfileMenu, ProfilerDriver.deepProfiling);
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(AddMonitorMenu, priority = 20)]
        private static void AddMonitor()
        {
            SimulationContext[] contexts = Object.FindObjectsByType<SimulationContext>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (contexts.Length == 0)
            {
                Debug.LogWarning("[SimulationProfiling] No hay SimulationContext en las escenas abiertas.");
                return;
            }

            GameObject target = contexts[0].gameObject;
            var monitor = target.GetComponent<SimulationAllocationMonitor>();
            if (monitor == null)
                monitor = Undo.AddComponent<SimulationAllocationMonitor>(target);

            Selection.activeObject = monitor;
            EditorGUIUtility.PingObject(monitor);
        }

        [MenuItem(AddMonitorMenu, true)]
        private static bool AddMonitorValidate()
        {
            return !EditorApplication.isPlaying;
        }
    }
}
