using System;
using UnityEngine;

namespace CircuitLab.Core
{
    /// <summary>Simni ulash urinishi natijasi.</summary>
    public enum WiringResult
    {
        /// <summary>Birinchi terminal tanlandi.</summary>
        Started = 0,
        /// <summary>Sim yaratildi.</summary>
        Connected = 1,
        /// <summary>Mavjud sim olib tashlandi (shu ikki terminalni qayta tanlash).</summary>
        Disconnected = 2,
        /// <summary>Tanlov bekor qilindi.</summary>
        Cancelled = 3,
        /// <summary>Ulab bo'lmadi (limit).</summary>
        Rejected = 4
    }

    /// <summary>
    /// "Ikki terminalni ketma-ket tanlash" ssenariysi (holat mashinasi). XR'dan mustaqil:
    /// terminal <see cref="CircuitTerminal.OnSelect"/> qaysi kontrollerdan kelganini bilmaydi.
    /// </summary>
    /// <remarks>
    /// 1) A terminal → Pending. 2) B terminal → sim yaratiladi; agar A–B simi bor bo'lsa — uziladi.
    /// 3) A'ni qayta tanlash → bekor qilish.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircuitBoard))]
    public sealed class CircuitWiringController : MonoBehaviour
    {
        private CircuitBoard _board;
        private CircuitTerminal _pending;

        /// <summary>Kutilayotgan (birinchi) terminal o'zgardi; null — tanlov yo'q.</summary>
        public event Action<CircuitTerminal> PendingTerminalChanged;

        /// <summary>Har bir ulash amali natijasi (UI/audio fikr-mulohaza uchun).</summary>
        public event Action<WiringResult> WiringPerformed;

        /// <summary>Kutilayotgan terminal.</summary>
        public CircuitTerminal PendingTerminal => _pending;

        private void Awake()
        {
            _board = GetComponent<CircuitBoard>();
        }

        /// <summary>Terminal tanlovini qayta ishlaydi.</summary>
        public void Select(CircuitTerminal terminal)
        {
            if (terminal == null) return;

            if (_pending == null)
            {
                SetPending(terminal);
                WiringPerformed?.Invoke(WiringResult.Started);
                return;
            }

            if (_pending == terminal)
            {
                Cancel();
                return;
            }

            CircuitTerminal first = _pending;
            SetPending(null);

            WireConnection existing = _board.FindWire(first, terminal);
            if (existing != null)
            {
                _board.Disconnect(existing);
                WiringPerformed?.Invoke(WiringResult.Disconnected);
                return;
            }

            WireConnection wire = _board.Connect(first, terminal);
            WiringPerformed?.Invoke(wire != null ? WiringResult.Connected : WiringResult.Rejected);
        }

        /// <summary>Joriy tanlovni bekor qiladi.</summary>
        public void Cancel()
        {
            if (_pending == null) return;
            SetPending(null);
            WiringPerformed?.Invoke(WiringResult.Cancelled);
        }

        private void SetPending(CircuitTerminal terminal)
        {
            if (_pending != null) _pending.SetPending(false);
            _pending = terminal;
            if (_pending != null) _pending.SetPending(true);
            PendingTerminalChanged?.Invoke(_pending);
        }
    }
}
