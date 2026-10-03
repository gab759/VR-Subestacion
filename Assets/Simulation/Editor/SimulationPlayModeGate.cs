using UnityEditor;

namespace VRSubestacion.EditorTools
{
    /// <summary>
    /// Valida el cableado al pulsar Play y, si está activado, cancela la entrada a Play Mode ante errores.
    /// </summary>
    [InitializeOnLoad]
    public static class SimulationPlayModeGate
    {
        private const string MenuRoot = "VR Subestacion/Validation/";
        private const string ValidateMenu = MenuRoot + "Validate Simulation Wiring";
        private const string CreateChannelsMenu = MenuRoot + "Create Missing Channel Assets";
        private const string BlockMenu = MenuRoot + "Block Play Mode On Wiring Errors";
        private const string BlockPrefKey = "VRSubestacion.BlockPlayOnWiringErrors";
        private const string LogHeader = "SimulationWiring";

        static SimulationPlayModeGate()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static bool BlockOnErrors
        {
            get => EditorPrefs.GetBool(BlockPrefKey, true);
            set => EditorPrefs.SetBool(BlockPrefKey, value);
        }

        [MenuItem(ValidateMenu, priority = 0)]
        public static void ValidateFromMenu()
        {
            WiringReport report = SimulationWiringValidator.ValidateAll();
            report.LogToConsole(LogHeader);
        }

        [MenuItem(CreateChannelsMenu, priority = 1)]
        public static void CreateChannelsFromMenu()
        {
            SimulationWiringValidator.CreateMissingChannelAssets();
        }

        [MenuItem(BlockMenu, priority = 20)]
        private static void ToggleBlock()
        {
            BlockOnErrors = !BlockOnErrors;
        }

        [MenuItem(BlockMenu, true)]
        private static bool ToggleBlockValidate()
        {
            Menu.SetChecked(BlockMenu, BlockOnErrors);
            return true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            WiringReport report = SimulationWiringValidator.ValidateAll();

            if (report.ErrorCount == 0 && report.WarningCount == 0)
                return;

            report.LogToConsole(LogHeader);

            if (report.HasErrors && BlockOnErrors)
            {
                EditorApplication.isPlaying = false;
                UnityEngine.Debug.LogError($"[{LogHeader}] Play Mode cancelado por errores de cableado. Desactiva '{BlockMenu}' para omitir.");
            }
        }
    }
}
