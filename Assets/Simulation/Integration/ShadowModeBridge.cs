using System;
using UnityEngine;

namespace VRSubestacion.Simulation.Integration
{
    public enum ArchitectureMode
    {
        /// <summary>Solo el flujo anterior (CheckList / SubstationProcedureManager). Valor seguro por defecto.</summary>
        Legacy = 0,

        /// <summary>Ambos flujos activos en paralelo; la arquitectura nueva observa sin reemplazar.</summary>
        Shadow = 1,

        /// <summary>Solo la arquitectura nueva.</summary>
        Clean = 2
    }

    public static class ArchitectureModeRules
    {
        public static bool IsLegacyActive(ArchitectureMode mode)
        {
            return mode != ArchitectureMode.Clean;
        }

        public static bool IsCleanActive(ArchitectureMode mode)
        {
            return mode != ArchitectureMode.Legacy;
        }

        /// <summary>
        /// Defines de Player Settings: VRSUB_FORCE_LEGACY o VRSUB_FORCE_CLEAN anulan el Inspector.
        /// </summary>
        public static ArchitectureMode ApplyCompileOverride(ArchitectureMode mode)
        {
#if VRSUB_FORCE_LEGACY
            return ArchitectureMode.Legacy;
#elif VRSUB_FORCE_CLEAN
            return ArchitectureMode.Clean;
#else
            return mode;
#endif
        }

        public static bool HasCompileOverride
        {
            get
            {
#if VRSUB_FORCE_LEGACY || VRSUB_FORCE_CLEAN
                return true;
#else
                return false;
#endif
            }
        }
    }

    /// <summary>
    /// Alterna entre arquitecturas habilitando/deshabilitando componentes y objetos ya existentes.
    /// No crea, destruye ni re-parenta nada. Se ejecuta antes que <see cref="SimulationContext"/>
    /// para que, en modo Legacy, el contexto nunca llegue a enlazarse.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [DisallowMultipleComponent]
    [AddComponentMenu("VR Subestacion/Shadow Mode Bridge")]
    public sealed class ShadowModeBridge : MonoBehaviour
    {
        public const int ExecutionOrder = -1000;

        [SerializeField] private ArchitectureMode mode = ArchitectureMode.Legacy;

        [Header("Arquitectura anterior")]
        [Tooltip("Ej.: CheckList, SubstationProcedureManager, ChecklistInteractable.")]
        [SerializeField] private Behaviour[] legacyBehaviours = Array.Empty<Behaviour>();
        [Tooltip("Ej.: ChecklistPanel del CheckList anterior.")]
        [SerializeField] private GameObject[] legacyObjects = Array.Empty<GameObject>();

        [Header("Arquitectura nueva")]
        [Tooltip("Ej.: SimulationContext, ChecklistPresenter, adaptadores Meta XR.")]
        [SerializeField] private Behaviour[] cleanBehaviours = Array.Empty<Behaviour>();
        [Tooltip("Ej.: panel nuevo con las filas TmpChecklistItemView.")]
        [SerializeField] private GameObject[] cleanObjects = Array.Empty<GameObject>();

        private ArchitectureMode _appliedMode;
        private bool _applied;

        public ArchitectureMode Mode => mode;
        public ArchitectureMode EffectiveMode => ArchitectureModeRules.ApplyCompileOverride(mode);
        public bool IsApplied => _applied;
        public ArchitectureMode AppliedMode => _appliedMode;
        public Behaviour[] LegacyBehaviours => legacyBehaviours;
        public GameObject[] LegacyObjects => legacyObjects;
        public Behaviour[] CleanBehaviours => cleanBehaviours;
        public GameObject[] CleanObjects => cleanObjects;

        public void Configure(
            Behaviour[] legacy,
            GameObject[] legacyGameObjects,
            Behaviour[] clean,
            GameObject[] cleanGameObjects)
        {
            legacyBehaviours = legacy ?? Array.Empty<Behaviour>();
            legacyObjects = legacyGameObjects ?? Array.Empty<GameObject>();
            cleanBehaviours = clean ?? Array.Empty<Behaviour>();
            cleanObjects = cleanGameObjects ?? Array.Empty<GameObject>();
        }

        public void SetMode(ArchitectureMode newMode)
        {
            mode = newMode;
            Apply();
        }

        public void Apply()
        {
            ArchitectureMode effective = EffectiveMode;
            bool legacyActive = ArchitectureModeRules.IsLegacyActive(effective);
            bool cleanActive = ArchitectureModeRules.IsCleanActive(effective);

            // Primero se apaga lo que sale y luego se enciende lo que entra,
            // para que nunca haya dos dueños activos de la misma UI durante el cambio.
            if (!legacyActive)
                SetGroup(legacyBehaviours, legacyObjects, false);
            if (!cleanActive)
                SetGroup(cleanBehaviours, cleanObjects, false);
            if (legacyActive)
                SetGroup(legacyBehaviours, legacyObjects, true);
            if (cleanActive)
                SetGroup(cleanBehaviours, cleanObjects, true);

            _appliedMode = effective;
            _applied = true;
        }

        private void Awake()
        {
            Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && _applied && _appliedMode != EffectiveMode)
                Apply();
        }
#endif

        private void SetGroup(Behaviour[] behaviours, GameObject[] objects, bool active)
        {
            if (behaviours != null)
            {
                for (int i = 0; i < behaviours.Length; i++)
                {
                    Behaviour b = behaviours[i];
                    if (b != null && !ReferenceEquals(b, this) && b.enabled != active)
                        b.enabled = active;
                }
            }

            if (objects != null)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    GameObject go = objects[i];
                    if (go != null && go != gameObject && go.activeSelf != active)
                        go.SetActive(active);
                }
            }
        }
    }
}
