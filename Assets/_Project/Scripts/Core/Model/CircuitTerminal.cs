using System;
using CircuitLab.Core.Interaction;
using UnityEngine;

namespace CircuitLab.Core
{
    /// <summary>Terminalning vizual holati (ko'rinish qatlami shu holatga obuna bo'ladi).</summary>
    public enum TerminalVisualState
    {
        /// <summary>Oddiy holat.</summary>
        Idle = 0,
        /// <summary>Kursor/nigoh ustida.</summary>
        Hovered = 1,
        /// <summary>Simning birinchi uchi sifatida tanlangan.</summary>
        Pending = 2
    }

    /// <summary>
    /// Komponentning ulanish qisqichi (graf tuguni). Sim faqat terminallar orasida tortiladi.
    /// Collider "CircuitInteractable" layer'ida bo'lishi kerak.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class CircuitTerminal : MonoBehaviour, ICircuitInteractable
    {
        /// <summary>Terminal qutbi.</summary>
        public enum Polarity
        {
            /// <summary>"+" yoki A uchi.</summary>
            Positive = 0,
            /// <summary>"−" yoki B uchi.</summary>
            Negative = 1
        }

        [SerializeField] private Polarity polarity = Polarity.Positive;

        [Tooltip("Sim uchi biriktiriladigan nuqta. Bo'sh bo'lsa — terminal markazi.")]
        [SerializeField] private Transform wireAnchor;

        private CircuitWiringController _wiring;
        private bool _hovered;
        private bool _pending;

        /// <summary>Vizual holat o'zgarganda.</summary>
        public event Action<CircuitTerminal, TerminalVisualState> VisualStateChanged;

        /// <summary>Terminal tegishli komponent.</summary>
        public CircuitComponent Owner { get; private set; }

        /// <summary>Terminal qutbi.</summary>
        public Polarity TerminalPolarity => polarity;

        /// <summary>Solver'dagi tugun indeksi (-1 — ro'yxatdan o'tmagan).</summary>
        public int NodeIndex { get; internal set; } = -1;

        /// <summary>Oxirgi yechimdagi potensial (V).</summary>
        public float Voltage { get; internal set; }

        /// <summary>Shu terminalga ulangan simlar soni.</summary>
        public int WireCount { get; internal set; }

        /// <summary>Simning dunyo koordinatalaridagi biriktirish nuqtasi.</summary>
        public Vector3 WirePoint => wireAnchor != null ? wireAnchor.position : transform.position;

        /// <summary>Hozirgi vizual holat.</summary>
        public TerminalVisualState VisualState =>
            _pending ? TerminalVisualState.Pending : _hovered ? TerminalVisualState.Hovered : TerminalVisualState.Idle;

        /// <inheritdoc />
        public bool IsInteractable => isActiveAndEnabled && Owner != null && _wiring != null;

        private void Awake()
        {
            _wiring = GetComponentInParent<CircuitWiringController>();
        }

        internal void Bind(CircuitComponent owner)
        {
            Owner = owner;
            if (_wiring == null) _wiring = GetComponentInParent<CircuitWiringController>();
        }

        /// <inheritdoc />
        public void OnHoverEnter()
        {
            _hovered = true;
            RaiseVisualState();
        }

        /// <inheritdoc />
        public void OnHoverExit()
        {
            _hovered = false;
            RaiseVisualState();
        }

        /// <inheritdoc />
        public void OnSelect()
        {
            if (IsInteractable) _wiring.Select(this);
        }

        internal void SetPending(bool pending)
        {
            if (_pending == pending) return;
            _pending = pending;
            RaiseVisualState();
        }

        private void RaiseVisualState()
        {
            VisualStateChanged?.Invoke(this, VisualState);
        }

        private void OnDisable()
        {
            _hovered = false;
            if (_pending && _wiring != null) _wiring.Cancel();
        }
    }
}
