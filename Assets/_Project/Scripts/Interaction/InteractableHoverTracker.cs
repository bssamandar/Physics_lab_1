using System;
using CircuitLab.Core.Interaction;
using UnityEngine;

namespace CircuitLab.Interaction
{
    /// <summary>
    /// AR va VR kontrollerlari uchun umumiy hover mantig'i: collider → <see cref="ICircuitInteractable"/>,
    /// OnHoverEnter/OnHoverExit chaqiruvlarini to'g'ri tartibda bajaradi.
    /// Interfeys qidiruvi faqat collider o'zgarganda bajariladi (har kadr emas).
    /// </summary>
    public sealed class InteractableHoverTracker
    {
        private Collider _lastCollider;
        private ICircuitInteractable _current;

        /// <summary>Hover maqsadi o'zgardi (null — maqsad yo'q).</summary>
        public event Action<ICircuitInteractable> TargetChanged;

        /// <summary>Joriy maqsad.</summary>
        public ICircuitInteractable Current => IsAlive(_current) ? _current : null;

        /// <summary>Maqsad bormi.</summary>
        public bool HasTarget => Current != null;

        /// <summary>Raycast natijasini beradi (hech narsaga tegmasa null).</summary>
        public void UpdateTarget(Collider hitCollider)
        {
            if (hitCollider == _lastCollider) return;
            _lastCollider = hitCollider;

            ICircuitInteractable next = hitCollider != null ? Resolve(hitCollider) : null;
            if (next != null && !next.IsInteractable) next = null;
            SetTarget(next);
        }

        /// <summary>Maqsadni tozalaydi (OnHoverExit chaqiriladi).</summary>
        public void Clear()
        {
            _lastCollider = null;
            SetTarget(null);
        }

        /// <summary>Joriy maqsadni tanlaydi. Muvaffaqiyatli bo'lsa true.</summary>
        public bool SelectCurrent()
        {
            ICircuitInteractable target = Current;
            if (target == null || !target.IsInteractable) return false;
            target.OnSelect();
            return true;
        }

        private void SetTarget(ICircuitInteractable next)
        {
            if (ReferenceEquals(next, _current)) return;
            if (IsAlive(_current)) _current.OnHoverExit();
            _current = next;
            if (_current != null) _current.OnHoverEnter();
            TargetChanged?.Invoke(_current);
        }

        private static ICircuitInteractable Resolve(Collider c)
        {
            if (c.TryGetComponent(out ICircuitInteractable direct)) return direct;
            return c.GetComponentInParent<ICircuitInteractable>();
        }

        private static bool IsAlive(ICircuitInteractable i)
        {
            // Yo'q qilingan Unity obyekti C# null emas — UnityEngine.Object operatori orqali tekshiramiz.
            if (i == null) return false;
            UnityEngine.Object o = i as UnityEngine.Object;
            return ReferenceEquals(o, null) || o != null;
        }
    }
}
