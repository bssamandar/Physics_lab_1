using System;
using System.Collections.Generic;
using CircuitLab.Core.Solver;
using UnityEngine;
using UnityEngine.Pool;

namespace CircuitLab.Core
{
    /// <summary>
    /// Laboratoriya stendi: komponentlar va simlar grafining egasi.
    /// Graf o'zgarganda "dirty" bayrog'ini qo'yadi va LateUpdate'da bir marta <see cref="CircuitSolver"/> ni chaqiradi,
    /// so'ng natijalarni hodisalar orqali tarqatadi. XR qatlamidan to'liq mustaqil.
    /// </summary>
    /// <remarks>
    /// Har bir terminal alohida tugun; simlar kichik qarshilikli (<see cref="wireResistance"/>) qirralar.
    /// Bu sim tokini ham hisoblash va qisqa tutashuvni fizik to'g'ri aniqlash imkonini beradi.
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class CircuitBoard : MonoBehaviour
    {
        [Tooltip("Bitta simning qarshiligi, Om.")]
        [SerializeField, Min(1e-4f)] private float wireResistance = 0.01f;

        [Tooltip("Stenddagi maksimal simlar soni (pool hajmi).")]
        [SerializeField, Min(1)] private int maxWires = 48;

        private readonly List<CircuitComponent> _components = new List<CircuitComponent>(16);
        private readonly List<CircuitTerminal> _terminals = new List<CircuitTerminal>(32);
        private readonly List<WireConnection> _wires = new List<WireConnection>(32);
        private readonly CircuitSolver _solver = new CircuitSolver();

        private CircuitElement[] _elements = Array.Empty<CircuitElement>();
        private int[] _componentElementIndex = Array.Empty<int>();
        private ObjectPool<WireConnection> _wirePool;
        private Action<CircuitComponent> _onComponentConfigChanged;

        private bool _topologyDirty;
        private bool _solveDirty;

        /// <summary>Yangi sim qo'shildi.</summary>
        public event Action<WireConnection> WireAdded;
        /// <summary>Sim olib tashlandi (shundan keyin obyekt pool'ga qaytadi).</summary>
        public event Action<WireConnection> WireRemoved;
        /// <summary>Zanjir qayta hisoblandi; barcha komponent va simlarda yangi qiymatlar.</summary>
        public event Action<CircuitBoard> Solved;
        /// <summary>Qisqa tutashuv holati o'zgardi. Ikkinchi parametr — sababchi batareya (yoki null).</summary>
        public event Action<bool, CircuitComponent> ShortCircuitChanged;

        /// <summary>Faol simlar.</summary>
        public IReadOnlyList<WireConnection> Wires => _wires;
        /// <summary>Ro'yxatdagi komponentlar.</summary>
        public IReadOnlyList<CircuitComponent> Components => _components;
        /// <summary>Hozir qisqa tutashuv bormi.</summary>
        public bool IsShortCircuited { get; private set; }
        /// <summary>Qisqa tutashuvga sababchi batareya.</summary>
        public CircuitComponent ShortCircuitSource { get; private set; }
        /// <summary>Maksimal simlar soni.</summary>
        public int MaxWires => maxWires;

        private void Awake()
        {
            if (_wirePool != null) return;
            _onComponentConfigChanged = HandleComponentConfigurationChanged;
            _wirePool = new ObjectPool<WireConnection>(
                createFunc: () => new WireConnection(),
                actionOnRelease: w => w.Unbind(),
                collectionCheck: false,
                defaultCapacity: maxWires,
                maxSize: maxWires);
        }

        private void LateUpdate()
        {
            if (_topologyDirty) RebuildTopology();
            if (_solveDirty) SolveNow();
        }

        #region Registry

        /// <summary>Komponentni grafga qo'shadi (CircuitComponent.OnEnable chaqiradi).</summary>
        internal void Register(CircuitComponent component)
        {
            if (component == null || _components.Contains(component)) return;
            EnsureInitialized();
            _components.Add(component);
            component.ConfigurationChanged += _onComponentConfigChanged;
            _topologyDirty = true;
        }

        /// <summary>Komponentni grafdan chiqaradi va unga ulangan simlarni uzadi.</summary>
        internal void Unregister(CircuitComponent component)
        {
            if (!_components.Remove(component)) return;
            component.ConfigurationChanged -= _onComponentConfigChanged;
            DisconnectAll(component.TerminalA);
            DisconnectAll(component.TerminalB);
            _topologyDirty = true;
        }

        #endregion

        #region Wiring API

        /// <summary>Ikki terminal orasida sim yaratadi.</summary>
        /// <returns>Yangi sim yoki null (bir xil terminal, takroriy sim, limit).</returns>
        public WireConnection Connect(CircuitTerminal a, CircuitTerminal b)
        {
            if (a == null || b == null || a == b || a.Owner == null || b.Owner == null) return null;
            if (a.Owner.Board != this || b.Owner.Board != this) return null;
            if (_wires.Count >= maxWires || FindWire(a, b) != null) return null;

            EnsureInitialized();
            WireConnection wire = _wirePool.Get();
            wire.Bind(a, b);
            _wires.Add(wire);
            a.WireCount++;
            b.WireCount++;
            _solveDirty = true;
            WireAdded?.Invoke(wire);
            return wire;
        }

        /// <summary>Simni olib tashlaydi.</summary>
        public bool Disconnect(WireConnection wire)
        {
            if (wire == null || !wire.IsActive) return false;
            int index = _wires.IndexOf(wire);
            if (index < 0) return false;

            _wires.RemoveAt(index);
            wire.TerminalA.WireCount--;
            wire.TerminalB.WireCount--;
            _solveDirty = true;
            WireRemoved?.Invoke(wire);
            _wirePool.Release(wire);
            return true;
        }

        /// <summary>Terminalga ulangan barcha simlarni uzadi.</summary>
        public void DisconnectAll(CircuitTerminal terminal)
        {
            if (terminal == null) return;
            for (int i = _wires.Count - 1; i >= 0; i--)
            {
                if (_wires[i].Touches(terminal)) Disconnect(_wires[i]);
            }
        }

        /// <summary>Barcha simlarni uzadi.</summary>
        public void ClearWires()
        {
            for (int i = _wires.Count - 1; i >= 0; i--) Disconnect(_wires[i]);
        }

        /// <summary>Ikki terminal orasidagi simni qidiradi.</summary>
        public WireConnection FindWire(CircuitTerminal a, CircuitTerminal b)
        {
            for (int i = 0; i < _wires.Count; i++)
            {
                if (_wires[i].Connects(a, b)) return _wires[i];
            }
            return null;
        }

        /// <summary>Keyingi kadrda qayta hisoblashni talab qiladi.</summary>
        public void MarkDirty()
        {
            _solveDirty = true;
        }

        #endregion

        #region Simulation

        private void EnsureInitialized()
        {
            if (_wirePool == null) Awake();
        }

        private void HandleComponentConfigurationChanged(CircuitComponent _)
        {
            _solveDirty = true;
        }

        /// <summary>Terminallarga tugun indekslarini beradi va buferlarni o'lchaydi (faqat graf o'zgarganda).</summary>
        private void RebuildTopology()
        {
            _topologyDirty = false;
            _terminals.Clear();

            for (int i = 0; i < _components.Count; i++)
            {
                AddTerminal(_components[i].TerminalA);
                AddTerminal(_components[i].TerminalB);
            }

            int elementCapacity = _components.Count + maxWires;
            if (_elements.Length < elementCapacity) _elements = new CircuitElement[elementCapacity];
            if (_componentElementIndex.Length < _components.Count) _componentElementIndex = new int[_components.Count];
            _solver.EnsureCapacity(_terminals.Count);
            _solveDirty = true;
        }

        private void AddTerminal(CircuitTerminal t)
        {
            if (t == null) return;
            t.NodeIndex = _terminals.Count;
            _terminals.Add(t);
        }

        /// <summary>Darhol qayta hisoblaydi (odatda LateUpdate o'zi chaqiradi).</summary>
        public void SolveNow()
        {
            if (_topologyDirty) RebuildTopology();
            _solveDirty = false;

            int count = 0;
            int ground = 0;
            bool groundFound = false;

            for (int i = 0; i < _components.Count; i++)
            {
                CircuitComponent c = _components[i];
                if (c.TryBuildElement(out CircuitElement element))
                {
                    _componentElementIndex[i] = count;
                    _elements[count++] = element;
                    if (!groundFound && c.Kind == ComponentKind.Battery)
                    {
                        ground = element.NodeB;
                        groundFound = true;
                    }
                }
                else
                {
                    _componentElementIndex[i] = -1;
                }
            }

            double wireConductance = 1d / wireResistance;
            int firstWireElement = count;
            for (int i = 0; i < _wires.Count; i++)
            {
                WireConnection w = _wires[i];
                _elements[count++] = new CircuitElement(w.TerminalA.NodeIndex, w.TerminalB.NodeIndex, wireConductance);
            }

            bool ok = _terminals.Count == 0 || _solver.Solve(_elements, count, _terminals.Count, ground);

            for (int i = 0; i < _terminals.Count; i++)
                _terminals[i].Voltage = ok ? (float)_solver.GetNodeVoltage(i) : 0f;

            for (int i = 0; i < _wires.Count; i++)
                _wires[i].Current = ok ? (float)_solver.GetElementCurrent(in _elements[firstWireElement + i]) : 0f;

            CircuitComponent shortSource = null;
            for (int i = 0; i < _components.Count; i++)
            {
                CircuitComponent c = _components[i];
                int e = _componentElementIndex[i];
                bool has = ok && e >= 0;
                c.ApplySolution(has, has ? (float)_solver.GetElementCurrent(in _elements[e]) : 0f);

                if (shortSource == null && c.Kind == ComponentKind.Battery && c.IsOverloaded) shortSource = c;
            }

            bool isShort = shortSource != null;
            if (isShort != IsShortCircuited || shortSource != ShortCircuitSource)
            {
                IsShortCircuited = isShort;
                ShortCircuitSource = shortSource;
                ShortCircuitChanged?.Invoke(isShort, shortSource);
            }

            if (!ok) Debug.LogWarning("[CircuitBoard] Zanjir tenglamalar tizimi yechilmadi (singulyar matritsa).", this);
            Solved?.Invoke(this);
        }

        #endregion
    }
}
