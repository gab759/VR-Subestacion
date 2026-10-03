using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRSubestacion.Presentation;
using VRSubestacion.Simulation.Integration;

namespace VRSubestacion.Simulation.PlayModeTests
{
    /// <summary>
    /// Escena mínima equivalente a la de producción: canales SO, SimulationContext, ChecklistPresenter
    /// con una fila TMP por paso, emisor de señales y (opcional) ShadowModeBridge con un flujo legacy simulado.
    /// Se construye con la raíz inactiva para que Awake/OnEnable corran con todo ya cableado.
    /// </summary>
    internal sealed class SimulationRig
    {
        public const string CompletedText = "Procedimiento completado";

        public readonly GameObject Root;
        public readonly GameObject ChecklistCanvas;
        public readonly InteractionSignalChannel Input;
        public readonly ProcedureProgressChannel Progress;
        public readonly SimulationContext Context;
        public readonly ChecklistPresenter Presenter;
        public readonly TmpChecklistItemView[] Views;
        public readonly int[] StepIds;
        public readonly string[] Titles;
        public readonly TMP_Text ProgressLabel;
        public readonly TMP_Text ActiveStepLabel;
        public readonly ChecklistStyle Style = new ChecklistStyle();
        public readonly ScriptedSignalEmitter Emitter;

        public readonly ShadowModeBridge Bridge;
        public readonly Behaviour LegacyBehaviour;
        public readonly GameObject LegacyPanel;

        public SimulationBinder Binder => Context.Binder;
        public ProceduralDirector Director => Context.Binder.Director;
        public int StepCount => StepIds.Length;

        public static SimulationRig Create()
        {
            return new SimulationRig(false, ArchitectureMode.Clean);
        }

        public static SimulationRig CreateWithBridge(ArchitectureMode mode)
        {
            return new SimulationRig(true, mode);
        }

        private SimulationRig(bool withBridge, ArchitectureMode mode)
        {
            Root = new GameObject("SimulationRig");
            Root.SetActive(false);

            Input = ScriptableObject.CreateInstance<InteractionSignalChannel>();
            Input.name = "TestInteractionSignalChannel";
            Progress = ScriptableObject.CreateInstance<ProcedureProgressChannel>();
            Progress.name = "TestProcedureProgressChannel";

            ChecklistCanvas = new GameObject("ChecklistCanvas", typeof(RectTransform));
            ChecklistCanvas.transform.SetParent(Root.transform, false);
            ChecklistCanvas.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            ISimulationStep[] steps = SubstationProcedureFactory.CreateSteps();
            StepIds = new int[steps.Length];
            Titles = new string[steps.Length];
            Views = new TmpChecklistItemView[steps.Length];
            var rows = new ChecklistPresenter.Row[steps.Length];

            for (int i = 0; i < steps.Length; i++)
            {
                StepIds[i] = steps[i].StepId;
                Titles[i] = "Paso " + (i + 1);
                Views[i] = CreateRow(ChecklistCanvas.transform, i);
                rows[i] = new ChecklistPresenter.Row
                {
                    step = (ProcedureStep)StepIds[i],
                    view = Views[i],
                    title = Titles[i]
                };
            }

            ProgressLabel = CreateLabel(ChecklistCanvas.transform, "ProgressLabel");
            ActiveStepLabel = CreateLabel(ChecklistCanvas.transform, "ActiveStepLabel");

            Presenter = ChecklistCanvas.AddComponent<ChecklistPresenter>();
            Presenter.Configure(rows, ProgressLabel, ActiveStepLabel);

            var contextGo = new GameObject("SimulationContext");
            contextGo.transform.SetParent(Root.transform, false);
            Context = contextGo.AddComponent<SimulationContext>();
            Context.Configure(Input, Progress, new[] { Presenter });

            Emitter = Root.AddComponent<ScriptedSignalEmitter>();

            if (!withBridge)
                return;

            var legacyGo = new GameObject("LegacySubstationProcedureManager");
            legacyGo.transform.SetParent(Root.transform, false);
            LegacyBehaviour = legacyGo.AddComponent<ToggleSignalGate>();

            LegacyPanel = new GameObject("LegacyChecklistPanel");
            LegacyPanel.transform.SetParent(Root.transform, false);

            Bridge = Root.AddComponent<ShadowModeBridge>();
            Bridge.Configure(
                new[] { LegacyBehaviour },
                new[] { LegacyPanel },
                new Behaviour[] { Context, Presenter },
                new[] { ChecklistCanvas });
            Bridge.SetMode(mode);
        }

        public void Activate()
        {
            Root.SetActive(true);
        }

        public void Destroy()
        {
            Object.Destroy(Root);
            Object.Destroy(Input);
            Object.Destroy(Progress);
        }

        private TmpChecklistItemView CreateRow(Transform parent, int index)
        {
            var rowGo = new GameObject("Row_" + index, typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var label = rowGo.AddComponent<TextMeshProUGUI>();

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(rowGo.transform, false);
            var icon = iconGo.AddComponent<Image>();

            var view = rowGo.AddComponent<TmpChecklistItemView>();
            view.Configure(label, icon, Style);
            return view;
        }

        private static TMP_Text CreateLabel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<TextMeshProUGUI>();
        }
    }
}
