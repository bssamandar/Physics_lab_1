using System;
using CircuitLab.Core.Interaction;
using CircuitLab.Core.Solver;
using UnityEngine;

namespace CircuitLab.Core
{
    /// <summary>
    /// Zanjirning ikki qutbli komponenti (batareya, rezistor, lampochka, kalit, ampermetr, voltmetr).
    /// Fizik parametrlar <see cref="ComponentDataSO"/> dan olinadi, natijalar esa
    /// <see cref="StateChanged"/> hodisasi orqali vizual qatlamga uzatiladi.
    /// </summary>
    /// <remarks>
    /// Iyerarxiya: CircuitBoard → ... → CircuitComponent → (TerminalA, TerminalB).
    /// Komponent tanasi ham <see cref="ICircuitInteractable"/>: kalitni bosish uni ochadi/yopadi,
    /// kuygan lampochkani bosish uni almashtiradi.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CircuitComponent : MonoBehaviour, ICircuitInteractable
    {
        [SerializeField] private ComponentDataSO data;

        [Tooltip("Batareya uchun \"+\" qutb.")]
        [SerializeField] private CircuitTerminal terminalA;

        [Tooltip("Batareya uchun \"−\" qutb.")]
        [SerializeField] private CircuitTerminal terminalB;

        [Tooltip("Faqat Switch uchun: boshlang'ich holat.")]
        [SerializeField] private bool closedOnStart;

        private bool _closed;
        private bool _hovered;

        /// <summary>Yechimdan keyin (tok/kuchlanish yangilanganda) chaqiriladi.</summary>
        public event Action<CircuitComponent> StateChanged;

        /// <summary>Konfiguratsiya o'zgardi (kalit, kuyish) — qayta hisoblash kerak.</summary>
        public event Action<CircuitComponent> ConfigurationChanged;

        /// <summary>Hover holati o'zgardi (vizual ajratish uchun).</summary>
        public event Action<CircuitComponent, bool> HoverChanged;

        /// <summary>Parametrlar aktivi.</summary>
        public ComponentDataSO Data => data;
        /// <summary>Komponent turi.</summary>
        public ComponentKind Kind => data != null ? data.Kind : ComponentKind.Resistor;
        /// <summary>A terminal ("+").</summary>
        public CircuitTerminal TerminalA => terminalA;
        /// <summary>B terminal ("−").</summary>
        public CircuitTerminal TerminalB => terminalB;
        /// <summary>Ega bo'lgan laboratoriya stendi.</summary>
        public CircuitBoard Board { get; private set; }

        /// <summary>
        /// Komponent orqali tok (A). Passiv elementlar: A→B. Batareya: "+" qutbdan tashqi zanjirga chiqayotgan tok.
        /// </summary>
        public float Current { get; private set; }
        /// <summary>Kuchlanish tushuvi V(A) − V(B), Volt.</summary>
        public float VoltageDrop { get; private set; }
        /// <summary>Ajralayotgan quvvat |U·I|, Vatt.</summary>
        public float Power => Mathf.Abs(VoltageDrop * Current);
        /// <summary>Tok <see cref="ComponentDataSO.MaxCurrent"/> dan oshdi.</summary>
        public bool IsOverloaded { get; private set; }
        /// <summary>Komponent kuygan (zanjirni uzadi).</summary>
        public bool IsBroken { get; private set; }
        /// <summary>Kalit yopiqmi (boshqa turlar uchun doim true).</summary>
        public bool IsClosed => Kind != ComponentKind.Switch || _closed;
        /// <summary>Hozir hover ostidami.</summary>
        public bool IsHovered => _hovered;

        /// <inheritdoc />
        public bool IsInteractable =>
            isActiveAndEnabled && data != null && (Kind == ComponentKind.Switch || IsBroken);

        private void Awake()
        {
            _closed = closedOnStart;
            if (terminalA != null) terminalA.Bind(this);
            if (terminalB != null) terminalB.Bind(this);
            Board = GetComponentInParent<CircuitBoard>();
        }

        private void OnEnable()
        {
            if (data != null) data.Changed += HandleDataChanged;
            if (Board != null) Board.Register(this);
        }

        private void OnDisable()
        {
            if (data != null) data.Changed -= HandleDataChanged;
            if (Board != null) Board.Unregister(this);
        }

        /// <summary>Kalit holatini almashtiradi.</summary>
        public void ToggleSwitch()
        {
            if (Kind != ComponentKind.Switch) return;
            _closed = !_closed;
            ConfigurationChanged?.Invoke(this);
        }

        /// <summary>Kalitni aniq holatga o'rnatadi.</summary>
        public void SetSwitchClosed(bool closed)
        {
            if (Kind != ComponentKind.Switch || _closed == closed) return;
            _closed = closed;
            ConfigurationChanged?.Invoke(this);
        }

        /// <summary>Kuygan komponentni tiklaydi.</summary>
        public void Repair()
        {
            if (!IsBroken) return;
            IsBroken = false;
            IsOverloaded = false;
            ConfigurationChanged?.Invoke(this);
        }

        /// <summary>
        /// Solver elementini tuzadi. Ochiq kalit yoki kuygan komponent uchun false (graf qirrasi yo'q).
        /// </summary>
        internal bool TryBuildElement(out CircuitElement element)
        {
            element = default;
            if (data == null || terminalA == null || terminalB == null || IsBroken || !IsClosed) return false;

            int a = terminalA.NodeIndex;
            int b = terminalB.NodeIndex;
            if (a < 0 || b < 0) return false;

            double r = data.Resistance;
            element = Kind == ComponentKind.Battery
                ? CircuitElement.Source(a, b, data.Voltage, r)
                : CircuitElement.Resistor(a, b, r);
            return true;
        }

        /// <summary>Solver natijasini qabul qiladi.</summary>
        /// <param name="elementCurrent">Solver bo'yicha A→B tok; <paramref name="hasElement"/> false bo'lsa e'tiborsiz.</param>
        /// <param name="hasElement">Komponent grafda qirra sifatida qatnashdimi.</param>
        internal void ApplySolution(bool hasElement, float elementCurrent)
        {
            if (data == null || terminalA == null || terminalB == null) return;
            VoltageDrop = terminalA.Voltage - terminalB.Voltage;
            float current = hasElement ? elementCurrent : 0f;
            Current = Kind == ComponentKind.Battery ? -current : current;

            bool overloaded = Mathf.Abs(Current) > data.MaxCurrent;
            IsOverloaded = overloaded;
            StateChanged?.Invoke(this);

            if (overloaded && data.BreaksOnOverload && !IsBroken)
            {
                IsBroken = true;
                ConfigurationChanged?.Invoke(this);
            }
        }

        /// <inheritdoc />
        public void OnHoverEnter()
        {
            _hovered = true;
            HoverChanged?.Invoke(this, true);
        }

        /// <inheritdoc />
        public void OnHoverExit()
        {
            _hovered = false;
            HoverChanged?.Invoke(this, false);
        }

        /// <inheritdoc />
        public void OnSelect()
        {
            if (IsBroken) Repair();
            else if (Kind == ComponentKind.Switch) ToggleSwitch();
        }

        private void HandleDataChanged()
        {
            ConfigurationChanged?.Invoke(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (terminalA != null && terminalA == terminalB)
                Debug.LogError($"[{name}] TerminalA va TerminalB bir xil bo'lishi mumkin emas.", this);
        }
#endif
    }
}
