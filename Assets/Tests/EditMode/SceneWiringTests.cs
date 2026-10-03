using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VRSubestacion.EditorTools;
using VRSubestacion.Presentation;
using VRSubestacion.Simulation.Integration;

namespace VRSubestacion.Simulation.Tests
{
    public sealed class SceneWiringTests
    {
        private readonly List<Object> _created = new List<Object>();

        private InteractionSignalChannel _input;
        private ProcedureProgressChannel _progress;

        [SetUp]
        public void SetUp()
        {
            _input = Track(ScriptableObject.CreateInstance<InteractionSignalChannel>());
            _progress = Track(ScriptableObject.CreateInstance<ProcedureProgressChannel>());
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }
            _created.Clear();
        }

        // --- ShadowModeBridge ---

        [TestCase(ArchitectureMode.Legacy, true, false)]
        [TestCase(ArchitectureMode.Shadow, true, true)]
        [TestCase(ArchitectureMode.Clean, false, true)]
        public void ModeRules_ResolveWhichArchitectureIsActive(ArchitectureMode mode, bool legacy, bool clean)
        {
            Assert.AreEqual(legacy, ArchitectureModeRules.IsLegacyActive(mode));
            Assert.AreEqual(clean, ArchitectureModeRules.IsCleanActive(mode));
        }

#if !VRSUB_FORCE_LEGACY && !VRSUB_FORCE_CLEAN
        [TestCase(ArchitectureMode.Legacy, true, false)]
        [TestCase(ArchitectureMode.Shadow, true, true)]
        [TestCase(ArchitectureMode.Clean, false, true)]
        public void Bridge_Apply_TogglesOnlyConfiguredComponentsAndObjects(ArchitectureMode mode, bool legacy, bool clean)
        {
            var legacyBehaviour = NewGameObject("LegacyManager").AddComponent<ToggleSignalGate>();
            GameObject legacyPanel = NewGameObject("LegacyPanel");
            var cleanContext = NewGameObject("Context").AddComponent<SimulationContext>();
            GameObject cleanPanel = NewGameObject("CleanPanel");
            var untouched = NewGameObject("Untouched").AddComponent<ToggleSignalGate>();

            ShadowModeBridge bridge = NewGameObject("Bridge").AddComponent<ShadowModeBridge>();
            bridge.Configure(
                new Behaviour[] { legacyBehaviour },
                new[] { legacyPanel },
                new Behaviour[] { cleanContext },
                new[] { cleanPanel });

            bridge.SetMode(mode);

            Assert.IsTrue(bridge.IsApplied);
            Assert.AreEqual(mode, bridge.AppliedMode);
            Assert.AreEqual(legacy, legacyBehaviour.enabled);
            Assert.AreEqual(legacy, legacyPanel.activeSelf);
            Assert.AreEqual(clean, cleanContext.enabled);
            Assert.AreEqual(clean, cleanPanel.activeSelf);
            Assert.IsTrue(untouched.enabled, "Lo no listado no se toca.");
        }

        [Test]
        public void Bridge_SwitchingBackToLegacy_RestoresLegacyFlow()
        {
            var legacyBehaviour = NewGameObject("LegacyManager").AddComponent<ToggleSignalGate>();
            var cleanContext = NewGameObject("Context").AddComponent<SimulationContext>();
            ShadowModeBridge bridge = NewGameObject("Bridge").AddComponent<ShadowModeBridge>();
            bridge.Configure(new Behaviour[] { legacyBehaviour }, null, new Behaviour[] { cleanContext }, null);

            bridge.SetMode(ArchitectureMode.Clean);
            Assert.IsFalse(legacyBehaviour.enabled);

            bridge.SetMode(ArchitectureMode.Legacy);
            Assert.IsTrue(legacyBehaviour.enabled);
            Assert.IsFalse(cleanContext.enabled);
        }
#endif

        [Test]
        public void Bridge_NeverDisablesItself()
        {
            GameObject go = NewGameObject("Bridge");
            ShadowModeBridge bridge = go.AddComponent<ShadowModeBridge>();
            bridge.Configure(new Behaviour[] { bridge }, new[] { go }, null, null);

            bridge.SetMode(ArchitectureMode.Clean);

            Assert.IsTrue(bridge.enabled);
            Assert.IsTrue(go.activeSelf);
        }

        [Test]
        public void Bridge_DefaultsToLegacy()
        {
            ShadowModeBridge bridge = NewGameObject("Bridge").AddComponent<ShadowModeBridge>();
            Assert.AreEqual(ArchitectureMode.Legacy, bridge.Mode);
        }

        // --- SimulationContext ---

        [Test]
        public void Context_Configure_ExposesInjectedDependencies()
        {
            var presenter = NewGameObject("Presenter").AddComponent<ChecklistPresenter>();
            var context = NewGameObject("Context").AddComponent<SimulationContext>();

            context.Configure(_input, _progress, new[] { presenter });

            Assert.AreSame(_input, context.InputChannel);
            Assert.AreSame(_progress, context.ProgressChannel);
            Assert.AreEqual(1, context.Presenters.Length);
        }

        [Test]
        public void Context_InjectPresenters_AssignsProgressChannelAndSkipsNulls()
        {
            var a = NewGameObject("A").AddComponent<ChecklistPresenter>();
            var b = NewGameObject("B").AddComponent<ChecklistPresenter>();

            SimulationContext.InjectPresenters(new[] { a, null, b }, _progress);

            Assert.AreSame(_progress, a.ProgressChannel);
            Assert.AreSame(_progress, b.ProgressChannel);
        }

        // --- Validador ---

        [Test]
        public void Validator_ContextWithoutChannels_ReportsErrors()
        {
            var context = NewGameObject("Context").AddComponent<SimulationContext>();
            var report = new WiringReport();

            SimulationWiringValidator.ValidateContext(context, report, requirePersistentAssets: false);

            Assert.AreEqual(2, report.ErrorCount);
            Assert.IsTrue(report.Contains(WiringSeverity.Error, "InteractionSignalChannel"));
            Assert.IsTrue(report.Contains(WiringSeverity.Error, "ProcedureProgressChannel"));
        }

        [Test]
        public void Validator_NonPersistentChannels_AreErrorsWhenAssetsRequired()
        {
            var context = NewGameObject("Context").AddComponent<SimulationContext>();
            context.Configure(_input, _progress, null);
            var report = new WiringReport();

            SimulationWiringValidator.ValidateContext(context, report, requirePersistentAssets: true);

            Assert.AreEqual(2, report.ErrorCount);
            Assert.IsTrue(report.Contains(WiringSeverity.Error, "no es un asset"));
        }

        [Test]
        public void Validator_FullyWiredScene_HasNoErrors()
        {
            var presenter = NewGameObject("Presenter").AddComponent<ChecklistPresenter>();
            var context = NewGameObject("Context").AddComponent<SimulationContext>();
            context.Configure(_input, _progress, new[] { presenter });

            ShadowModeBridge bridge = NewGameObject("Bridge").AddComponent<ShadowModeBridge>();
            bridge.Configure(null, null, new Behaviour[] { context, presenter }, null);

            var report = new WiringReport();
            SimulationWiringValidator.ValidateScene(
                new[] { context }, new[] { presenter }, new[] { bridge }, report, requirePersistentAssets: false);

            Assert.AreEqual(0, report.ErrorCount);
            Assert.IsTrue(report.Contains(WiringSeverity.Warning, "no tiene filas"));
        }

        [Test]
        public void Validator_OrphanPresenterWithoutChannel_IsError()
        {
            var presenter = NewGameObject("Presenter").AddComponent<ChecklistPresenter>();
            var report = new WiringReport();

            SimulationWiringValidator.ValidatePresenter(presenter, new SimulationContext[0], report);

            Assert.IsTrue(report.Contains(WiringSeverity.Error, "ningún SimulationContext"));
        }

        [Test]
        public void Validator_PresenterWithDifferentChannel_IsWarning()
        {
            var other = Track(ScriptableObject.CreateInstance<ProcedureProgressChannel>());
            var presenter = NewGameObject("Presenter").AddComponent<ChecklistPresenter>();
            presenter.SetChannel(other);
            var context = NewGameObject("Context").AddComponent<SimulationContext>();
            context.Configure(_input, _progress, new[] { presenter });
            var report = new WiringReport();

            SimulationWiringValidator.ValidatePresenter(presenter, new[] { context }, report);

            Assert.AreEqual(0, report.ErrorCount);
            Assert.IsTrue(report.Contains(WiringSeverity.Warning, "difiere"));
        }

        [Test]
        public void Validator_BridgeInCleanModeWithoutContext_IsError()
        {
            ShadowModeBridge bridge = NewGameObject("Bridge").AddComponent<ShadowModeBridge>();
            bridge.SetMode(ArchitectureMode.Clean);
            var report = new WiringReport();

            SimulationWiringValidator.ValidateBridges(new[] { bridge }, new SimulationContext[0], report);

#if VRSUB_FORCE_LEGACY
            Assert.AreEqual(0, report.ErrorCount);
#else
            Assert.IsTrue(report.Contains(WiringSeverity.Error, "no hay SimulationContext"));
#endif
        }

        [Test]
        public void Validator_ContextMissingFromBridge_IsWarning()
        {
            var context = NewGameObject("Context").AddComponent<SimulationContext>();
            ShadowModeBridge bridge = NewGameObject("Bridge").AddComponent<ShadowModeBridge>();
            var report = new WiringReport();

            SimulationWiringValidator.ValidateBridges(new[] { bridge }, new[] { context }, report);

            Assert.IsTrue(report.Contains(WiringSeverity.Warning, "Clean Behaviours"));
        }

        [Test]
        public void Validator_MultipleBridges_IsError()
        {
            ShadowModeBridge a = NewGameObject("BridgeA").AddComponent<ShadowModeBridge>();
            ShadowModeBridge b = NewGameObject("BridgeB").AddComponent<ShadowModeBridge>();
            var report = new WiringReport();

            SimulationWiringValidator.ValidateBridges(new[] { a, b }, new SimulationContext[0], report);

            Assert.IsTrue(report.Contains(WiringSeverity.Error, "solo debe existir uno"));
        }

        [Test]
        public void Validator_ScanFindsAdapterEmittingToForeignChannel()
        {
            var foreign = Track(ScriptableObject.CreateInstance<InteractionSignalChannel>());
            foreign.name = "ForeignChannel";

            var good = NewGameObject("GoodGate").AddComponent<ToggleSignalGate>();
            var bad = NewGameObject("BadGate").AddComponent<ToggleSignalGate>();
            SetSignalOutputChannel(good, _input);
            SetSignalOutputChannel(bad, foreign);

            var report = new WiringReport();
            SimulationWiringValidator.ScanChannelReferences(
                new MonoBehaviour[] { good, bad }, _input, _progress, report);

            Assert.AreEqual(1, report.WarningCount);
            Assert.IsTrue(report.Contains(WiringSeverity.Warning, "ForeignChannel"));
            Assert.AreSame(bad, report.Issues[0].Context);
        }

        private static void SetSignalOutputChannel(ToggleSignalGate gate, InteractionSignalChannel channel)
        {
            using (var so = new SerializedObject(gate))
            {
                so.FindProperty("onAllOn.channel").objectReferenceValue = channel;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private GameObject NewGameObject(string name)
        {
            return Track(new GameObject(name));
        }

        private T Track<T>(T obj) where T : Object
        {
            _created.Add(obj);
            return obj;
        }
    }
}
