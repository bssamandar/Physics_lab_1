using CircuitLab.Core.Solver;
using NUnit.Framework;

namespace CircuitLab.Tests
{
    /// <summary>Matematik yadro testlari: Om qonuni, ketma-ket/parallel, qisqa tutashuv, uzilgan zanjir.</summary>
    public sealed class CircuitSolverTests
    {
        private const double Tolerance = 1e-4;
        private CircuitSolver _solver;
        private CircuitElement[] _elements;

        [SetUp]
        public void SetUp()
        {
            _solver = new CircuitSolver();
            _elements = new CircuitElement[8];
        }

        [Test]
        public void SingleResistor_FollowsOhmsLaw()
        {
            // 0: "−" (yer), 1: "+". E = 6 V, r → juda kichik, R = 3 Om → I ≈ 2 A.
            _elements[0] = CircuitElement.Source(1, 0, 6.0, 1e-6);
            _elements[1] = CircuitElement.Resistor(1, 0, 3.0);

            Assert.IsTrue(_solver.Solve(_elements, 2, 2, 0));
            Assert.AreEqual(6.0, _solver.GetNodeVoltage(1), Tolerance);
            Assert.AreEqual(2.0, _solver.GetElementCurrent(_elements[1]), Tolerance);
            Assert.AreEqual(-2.0, _solver.GetElementCurrent(_elements[0]), Tolerance);
        }

        [Test]
        public void SeriesResistors_SumResistanceAndDivideVoltage()
        {
            // E=9 V, R1=1 Om (1→2), R2=2 Om (2→0) → I=3 A, V(2)=6 V.
            _elements[0] = CircuitElement.Source(1, 0, 9.0, 1e-6);
            _elements[1] = CircuitElement.Resistor(1, 2, 1.0);
            _elements[2] = CircuitElement.Resistor(2, 0, 2.0);

            Assert.IsTrue(_solver.Solve(_elements, 3, 3, 0));
            Assert.AreEqual(3.0, _solver.GetElementCurrent(_elements[1]), Tolerance);
            Assert.AreEqual(3.0, _solver.GetElementCurrent(_elements[2]), Tolerance);
            Assert.AreEqual(6.0, _solver.GetNodeVoltage(2), Tolerance);
        }

        [Test]
        public void ParallelResistors_SplitCurrentByKirchhoff()
        {
            // E=12 V, R1=6 Om, R2=3 Om parallel → I1=2 A, I2=4 A, I=6 A.
            _elements[0] = CircuitElement.Source(1, 0, 12.0, 1e-6);
            _elements[1] = CircuitElement.Resistor(1, 0, 6.0);
            _elements[2] = CircuitElement.Resistor(1, 0, 3.0);

            Assert.IsTrue(_solver.Solve(_elements, 3, 2, 0));
            double i1 = _solver.GetElementCurrent(_elements[1]);
            double i2 = _solver.GetElementCurrent(_elements[2]);
            double source = -_solver.GetElementCurrent(_elements[0]);
            Assert.AreEqual(2.0, i1, Tolerance);
            Assert.AreEqual(4.0, i2, Tolerance);
            Assert.AreEqual(i1 + i2, source, Tolerance);
        }

        [Test]
        public void InternalResistance_DropsTerminalVoltage()
        {
            // E=4.5 V, r=0.5 Om, R=4 Om → I=1 A, U=4 V.
            _elements[0] = CircuitElement.Source(1, 0, 4.5, 0.5);
            _elements[1] = CircuitElement.Resistor(1, 0, 4.0);

            Assert.IsTrue(_solver.Solve(_elements, 2, 2, 0));
            Assert.AreEqual(1.0, _solver.GetElementCurrent(_elements[1]), Tolerance);
            Assert.AreEqual(4.0, _solver.GetNodeVoltage(1), Tolerance);
        }

        [Test]
        public void ShortCircuit_CurrentApproachesEmfOverInternalResistance()
        {
            // Sim (0.01 Om) batareya qutblarini tutashtiradi: I = 4.5 / (0.5 + 0.01) ≈ 8.82 A.
            _elements[0] = CircuitElement.Source(1, 0, 4.5, 0.5);
            _elements[1] = CircuitElement.Resistor(1, 0, 0.01);

            Assert.IsTrue(_solver.Solve(_elements, 2, 2, 0));
            Assert.AreEqual(4.5 / 0.51, -_solver.GetElementCurrent(_elements[0]), 1e-3);
        }

        [Test]
        public void OpenCircuit_HasNoCurrentAndFloatingNodesDoNotBreakSolver()
        {
            // Batareya + "osilib turgan" rezistor (2–3 tugunlar hech narsaga ulanmagan).
            _elements[0] = CircuitElement.Source(1, 0, 6.0, 0.1);
            _elements[1] = CircuitElement.Resistor(2, 3, 10.0);

            Assert.IsTrue(_solver.Solve(_elements, 2, 4, 0));
            Assert.AreEqual(0.0, _solver.GetElementCurrent(_elements[1]), Tolerance);
            Assert.AreEqual(6.0, _solver.GetNodeVoltage(1), 1e-3);
        }

        [Test]
        public void RepeatedSolve_ReusesBuffers()
        {
            _elements[0] = CircuitElement.Source(1, 0, 3.0, 0.1);
            _elements[1] = CircuitElement.Resistor(1, 0, 10.0);
            _solver.EnsureCapacity(4);

            for (int i = 0; i < 100; i++) Assert.IsTrue(_solver.Solve(_elements, 2, 2, 0));
            Assert.AreEqual(3.0 * 10.0 / 10.1, _solver.GetNodeVoltage(1), Tolerance);
        }
    }
}
