using System;
using NUnit.Framework;
using UnityEngine;
using VRSubestacion.Simulation;

namespace VRSubestacion.Simulation.Tests
{
    public sealed class ProceduralDirectorTests
    {
        private InteractionSignalChannel _channel;
        private ProceduralDirector _director;

        [SetUp]
        public void SetUp()
        {
            _channel = ScriptableObject.CreateInstance<InteractionSignalChannel>();
            _director = ProceduralDirector.CreateSubstationProcedure();
            _director.Bind(_channel);
            _director.StartProcedure();
        }

        [TearDown]
        public void TearDown()
        {
            if (_director != null)
                _director.Unbind();

            if (_channel != null)
                UnityEngine.Object.DestroyImmediate(_channel);
        }

        [Test]
        public void Factory_CreatesEighteenConcreteStepsInProcedureOrder()
        {
            ISimulationStep[] steps = SubstationProcedureFactory.CreateSteps();

            Assert.AreEqual(SubstationProcedureFactory.StepCount, steps.Length);
            Assert.AreEqual(typeof(BajarSwitchPrincipalStep), steps[0].GetType());
            Assert.AreEqual(typeof(BajarProtectoresStep), steps[1].GetType());
            Assert.AreEqual(typeof(ApagarSubestacionBotonRojoStep), steps[2].GetType());
            Assert.AreEqual(typeof(SubirProtectoresStep), steps[3].GetType());
            Assert.AreEqual(typeof(ColocarCandadoStep), steps[4].GetType());
            Assert.AreEqual(typeof(VerificarBarrasPuerta1AbajoStep), steps[5].GetType());
            Assert.AreEqual(typeof(VerificarBarrasPuerta1ArribaStep), steps[6].GetType());
            Assert.AreEqual(typeof(VerificarBarrasPuerta2Step), steps[7].GetType());
            Assert.AreEqual(typeof(VerificarChocolatitoStep), steps[8].GetType());
            Assert.AreEqual(typeof(BajarPalancasPuerta1Step), steps[9].GetType());
            Assert.AreEqual(typeof(ColocarPulpoTierraStep), steps[10].GetType());
            Assert.AreEqual(typeof(ColocarPulpoBarrasStep), steps[11].GetType());
            Assert.AreEqual(typeof(SenalizarZonaStep), steps[12].GetType());
            Assert.AreEqual(typeof(RetirarPulpoStep), steps[13].GetType());
            Assert.AreEqual(typeof(SubirPalancasPuerta1Step), steps[14].GetType());
            Assert.AreEqual(typeof(SubirSwitchPrincipalStep), steps[15].GetType());
            Assert.AreEqual(typeof(QuitarCandadoStep), steps[16].GetType());
            Assert.AreEqual(typeof(EncenderSubestacionBotonRojoStep), steps[17].GetType());

            for (int i = 0; i < steps.Length; i++)
            {
                Assert.AreEqual(SubstationProcedureFactory.OrderedSignalIds[i], steps[i].ExpectedSignalId);
                Assert.AreEqual(SubstationProcedureFactory.OrderedSignalIds[i], steps[i].StepId);
            }
        }

        [Test]
        public void StartProcedure_EntersFirstStep()
        {
            Assert.IsTrue(_director.IsStarted);
            Assert.IsFalse(_director.IsComplete);
            Assert.AreEqual(0, _director.CurrentStepIndex);
            Assert.AreEqual(ProcedureStepIds.BajarSwitchPrincipal, _director.CurrentStepId);
        }

        [Test]
        public void ChannelRaise_AdvancesOnlyWhenSignalMatchesCurrentStep()
        {
            _channel.Raise(InteractionSignalIds.ColocarCandado);
            Assert.AreEqual(0, _director.CurrentStepIndex);

            _channel.Raise(InteractionSignalIds.BajarSwitchPrincipal);
            Assert.AreEqual(1, _director.CurrentStepIndex);
            Assert.AreEqual(ProcedureStepIds.BajarProtectores, _director.CurrentStepId);
        }

        [Test]
        public void SequentialChannelSignals_AdvanceEveryStepUntilComplete()
        {
            int[] ids = SubstationProcedureFactory.OrderedSignalIds;

            for (int i = 0; i < ids.Length; i++)
            {
                Assert.IsFalse(_director.IsComplete);
                Assert.AreEqual(i, _director.CurrentStepIndex);
                Assert.AreEqual(ids[i], _director.CurrentExpectedSignalId);

                _channel.Raise(ids[i]);
            }

            Assert.IsTrue(_director.IsComplete);
            Assert.AreEqual(ids.Length, _director.CurrentStepIndex);
            Assert.AreEqual(ProcedureStepIds.None, _director.CurrentStepId);
        }

        [Test]
        public void OutOfOrderSignal_DoesNotSkipSteps()
        {
            _channel.Raise(InteractionSignalIds.BajarSwitchPrincipal);
            _channel.Raise(InteractionSignalIds.ApagarSubestacion_BotonRojo);
            _channel.Raise(InteractionSignalIds.SubirProtectores);

            Assert.AreEqual(1, _director.CurrentStepIndex);
            Assert.AreEqual(ProcedureStepIds.BajarProtectores, _director.CurrentStepId);
        }

        [Test]
        public void UnboundDirector_DoesNotAdvanceWhenChannelRaises()
        {
            var unbound = ProceduralDirector.CreateSubstationProcedure();
            unbound.StartProcedure();

            _channel.Raise(InteractionSignalIds.BajarSwitchPrincipal);

            Assert.AreEqual(0, unbound.CurrentStepIndex);
            Assert.AreEqual(1, _director.CurrentStepIndex);
        }

        [Test]
        public void UnstartedDirector_IgnoresChannelSignals()
        {
            var idle = ProceduralDirector.CreateSubstationProcedure();
            idle.Bind(_channel);

            _channel.Raise(InteractionSignalIds.BajarSwitchPrincipal);

            Assert.IsFalse(idle.IsStarted);
            Assert.AreEqual(0, idle.CurrentStepIndex);
            Assert.AreEqual(ProcedureStepIds.None, idle.CurrentStepId);

            idle.Unbind();
        }

        [Test]
        public void CompletedProcedure_IgnoresFurtherSignals()
        {
            int[] ids = SubstationProcedureFactory.OrderedSignalIds;
            for (int i = 0; i < ids.Length; i++)
                _channel.Raise(ids[i]);

            _channel.Raise(InteractionSignalIds.BajarSwitchPrincipal);

            Assert.IsTrue(_director.IsComplete);
            Assert.AreEqual(ids.Length, _director.CurrentStepIndex);
        }

        [Test]
        public void Tick_DoesNotAllocate()
        {
            _director.Tick(0.016f);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 64; i++)
                _director.Tick(0.016f);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.AreEqual(before, after, "Tick no debe asignar en heap.");
        }

        [Test]
        public void ChannelRaise_DoesNotAllocateAfterWarmup()
        {
            _channel.Raise(InteractionSignalIds.BajarProtectores);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 64; i++)
                _channel.Raise(InteractionSignalIds.BajarProtectores);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.AreEqual(before, after, "Raise del canal no debe asignar en heap.");
            Assert.AreEqual(0, _director.CurrentStepIndex);
        }
    }
}
