using System;
using CircuitLab.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CircuitLab.XR.VR
{
    /// <summary>
    /// Mobil VR (Cardboard / XR Interaction Toolkit) uchun nigoh kontrolleri.
    /// Kamera (yoki kontroller) markazidan Physics raycast, obyekt ustida <see cref="dwellTime"/> davomida
    /// turilsa — tanlov. Radial reticle (Image.fillAmount) to'lish animatsiyasi bilan.
    /// Ixtiyoriy <see cref="selectAction"/> (Cardboard tugmasi, ekran tap, kontroller trigger) darhol tanlaydi.
    /// </summary>
    /// <remarks>
    /// GC: RaycastNonAlloc buferi oldindan ajratilgan; Update'da xotira ajratilmaydi.
    /// Reticle — kamera ostidagi World Space Canvas (Image Type = Filled, Fill Method = Radial 360).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class VRGazeController : MonoBehaviour
    {
        [Header("Ray")]
        [Tooltip("Nur manbai: odatda Main Camera (gaze) yoki kontroller Transform'i.")]
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private LayerMask interactableMask = 1 << 8;
        [SerializeField, Min(0.1f)] private float maxDistance = 6f;

        [Header("Dwell")]
        [SerializeField, Min(0.1f)] private float dwellTime = 1.2f;
        [Tooltip("Nigoh obyektga tushgandan keyin to'lish boshlanguncha kechikish (tasodifiy tanlovni kamaytiradi).")]
        [SerializeField, Min(0f)] private float dwellDelay = 0.15f;
        [Tooltip("Tanlovdan keyin xuddi shu obyektni qayta tanlash uchun nigohni olib qochish shart.")]
        [SerializeField] private bool requireLookAwayToReselect = true;

        [Header("Reticle")]
        [SerializeField] private Transform reticle;
        [SerializeField] private Image reticleFill;
        [SerializeField, Min(0.05f)] private float idleReticleDistance = 2f;
        [Tooltip("Reticle sirtga yopishib qolmasligi uchun orqaga surish (m).")]
        [SerializeField, Min(0f)] private float surfaceOffset = 0.01f;
        [Tooltip("1 m masofadagi reticle o'lchami (burchak o'lchami doimiy saqlanadi).")]
        [SerializeField, Min(0.0001f)] private float reticleScalePerMeter = 0.02f;
        [SerializeField, Min(1f)] private float hoverScaleMultiplier = 1.6f;
        [SerializeField, Min(0.1f)] private float scaleLerpSpeed = 12f;

        [Header("Optional instant select")]
        [Tooltip("Masalan: <Touchscreen>/primaryTouch/tap, <XRController>{RightHand}/triggerPressed, <Keyboard>/space.")]
        [SerializeField] private InputActionReference selectAction;

        private readonly RaycastHit[] _hits = new RaycastHit[8];
        private readonly InteractableHoverTracker _hover = new InteractableHoverTracker();

        private Collider _currentCollider;
        private float _gazeTimer;
        private bool _selectedCurrent;
        private float _scaleMultiplier = 1f;

        /// <summary>Dwell progressi o'zgardi [0..1].</summary>
        public event Action<float> DwellProgressChanged;

        /// <summary>Obyekt tanlandi.</summary>
        public event Action Selected;

        /// <summary>Joriy dwell progressi [0..1].</summary>
        public float DwellProgress { get; private set; }

        private void Awake()
        {
            if (rayOrigin == null && Camera.main != null) rayOrigin = Camera.main.transform;
            if (reticleFill != null) reticleFill.fillAmount = 0f;
        }

        private void OnEnable()
        {
            if (selectAction != null && selectAction.action != null) selectAction.action.Enable();
        }

        private void OnDisable()
        {
            _hover.Clear();
            _currentCollider = null;
            SetFill(0f);
        }

        private void Update()
        {
            if (rayOrigin == null) return;

            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            bool hasHit = RaycastUtility.TryGetClosestHit(ray, _hits, maxDistance, interactableMask, out RaycastHit hit);
            Collider hitCollider = hasHit ? hit.collider : null;

            if (hitCollider != _currentCollider)
            {
                _currentCollider = hitCollider;
                _gazeTimer = 0f;
                _selectedCurrent = false;
            }

            _hover.UpdateTarget(hitCollider);
            bool hasTarget = _hover.HasTarget;

            UpdateDwell(hasTarget);
            UpdateReticle(ray, hasHit ? hit.distance : idleReticleDistance, hasTarget);
        }

        private void UpdateDwell(bool hasTarget)
        {
            if (!hasTarget)
            {
                SetFill(0f);
                return;
            }

            if (selectAction != null && selectAction.action != null && selectAction.action.WasPerformedThisFrame())
            {
                Select();
                return;
            }

            if (_selectedCurrent && requireLookAwayToReselect)
            {
                SetFill(0f);
                return;
            }

            _gazeTimer += Time.deltaTime;
            float progress = Mathf.Clamp01((_gazeTimer - dwellDelay) / dwellTime);
            SetFill(progress);

            if (progress >= 1f) Select();
        }

        private void Select()
        {
            if (!_hover.SelectCurrent()) return;
            _selectedCurrent = true;
            _gazeTimer = 0f;
            SetFill(0f);
            Selected?.Invoke();
        }

        private void UpdateReticle(Ray ray, float distance, bool hovering)
        {
            if (reticle == null) return;

            float d = Mathf.Max(0.05f, distance - surfaceOffset);
            reticle.position = ray.origin + ray.direction * d;
            reticle.rotation = Quaternion.LookRotation(ray.direction, rayOrigin.up);

            float target = hovering ? hoverScaleMultiplier : 1f;
            _scaleMultiplier = Mathf.MoveTowards(_scaleMultiplier, target, scaleLerpSpeed * Time.deltaTime);
            float s = d * reticleScalePerMeter * _scaleMultiplier;
            reticle.localScale = new Vector3(s, s, s);
        }

        private void SetFill(float value)
        {
            if (Mathf.Approximately(DwellProgress, value)) return;
            DwellProgress = value;
            if (reticleFill != null) reticleFill.fillAmount = value;
            DwellProgressChanged?.Invoke(value);
        }
    }
}
