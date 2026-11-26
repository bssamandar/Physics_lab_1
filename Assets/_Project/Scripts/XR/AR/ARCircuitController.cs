using System;
using System.Collections.Generic;
using CircuitLab.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace CircuitLab.XR.AR
{
    /// <summary>
    /// Mobil AR kontrolleri (AR Foundation: ARCore/ARKit).
    /// 1-bosqich (Scanning): gorizontal tekislik qidiriladi, ekran markazida joylashtirish indikatori ko'rsatiladi,
    /// tap — laboratoriya stendi <see cref="ARAnchor"/> ga biriktirilib joylashtiriladi.
    /// 2-bosqich (Placed): touch raycast orqali <see cref="CircuitLab.Core.Interaction.ICircuitInteractable"/> bilan ishlash
    /// (barmoq tekkanda hover, qo'yib yuborilganda o'sha obyekt ustida bo'lsa — select).
    /// </summary>
    /// <remarks>
    /// GC: ARRaycastHit ro'yxati va RaycastHit buferi oldindan ajratilgan, Update'da xotira ajratilmaydi.
    /// Talab: Input System (Active Input Handling = "Input System Package" yoki "Both").
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ARRaycastManager))]
    [RequireComponent(typeof(ARPlaneManager))]
    [RequireComponent(typeof(ARAnchorManager))]
    public sealed class ARCircuitController : MonoBehaviour
    {
        /// <summary>Kontroller holati.</summary>
        public enum State
        {
            /// <summary>Stol yuzasi qidirilmoqda.</summary>
            Scanning = 0,
            /// <summary>Stend joylashtirilgan.</summary>
            Placed = 1
        }

        [Header("Scene references")]
        [Tooltip("XR Origin ichidagi AR kamera.")]
        [SerializeField] private Camera arCamera;
        [Tooltip("Laboratoriya stendining ildiz obyekti (CircuitBoard shu yerda). Boshida o'chiq bo'lishi mumkin.")]
        [SerializeField] private Transform labRoot;
        [Tooltip("Ekran markazidagi joylashtirish indikatori (ixtiyoriy).")]
        [SerializeField] private Transform placementIndicator;

        [Header("Placement")]
        [SerializeField] private TrackableType placementTrackables = TrackableType.PlaneWithinPolygon;
        [Tooltip("Joylashtirilgandan keyin tekislik vizualizatsiyasini yashirish.")]
        [SerializeField] private bool hidePlanesAfterPlacement = true;
        [Tooltip("Stend kameraga qarab buriladi (faqat Y o'qi bo'yicha).")]
        [SerializeField] private bool faceCameraOnPlace = true;

        [Header("Interaction")]
        [SerializeField] private LayerMask interactableMask = 1 << 8;
        [SerializeField, Min(0.1f)] private float maxRayDistance = 10f;
        [Tooltip("UI ustidagi teginishlarni e'tiborsiz qoldirish.")]
        [SerializeField] private bool ignoreTouchesOverUI = true;

        private static readonly List<ARRaycastHit> s_arHits = new List<ARRaycastHit>(8);
        private readonly RaycastHit[] _physicsHits = new RaycastHit[8];
        private readonly InteractableHoverTracker _hover = new InteractableHoverTracker();

        private ARRaycastManager _raycastManager;
        private ARPlaneManager _planeManager;
        private Transform _cameraTransform;
        private GameObject _anchorObject;
        private Collider _pressedCollider;
        private int _activeTouchId = -1;
        private State _state = State.Scanning;

        /// <summary>Stend joylashtirildi (yangi anchor bilan).</summary>
        public event Action<Transform> LabPlaced;

        /// <summary>Holat o'zgardi.</summary>
        public event Action<State> StateChanged;

        /// <summary>Joriy holat.</summary>
        public State CurrentState => _state;

        private void Awake()
        {
            _raycastManager = GetComponent<ARRaycastManager>();
            _planeManager = GetComponent<ARPlaneManager>();
            if (arCamera == null) arCamera = Camera.main;
            _cameraTransform = arCamera != null ? arCamera.transform : null;
            if (labRoot != null) labRoot.gameObject.SetActive(false);
            if (placementIndicator != null) placementIndicator.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            _hover.Clear();
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            if (_cameraTransform == null) return;

            if (_state == State.Scanning) UpdateScanning();
            else UpdateInteraction();
        }

        #region Scanning / placement

        private void UpdateScanning()
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            bool hasPose = TryGetPlanePose(center, out Pose indicatorPose);

            if (placementIndicator != null)
            {
                if (placementIndicator.gameObject.activeSelf != hasPose) placementIndicator.gameObject.SetActive(hasPose);
                if (hasPose) placementIndicator.SetPositionAndRotation(indicatorPose.position, YawTowardsCamera(indicatorPose.position));
            }

            if (!TryGetTouchBegan(out Touch touch)) return;

            // Teginish nuqtasida tekislik bo'lsa — o'sha yerga, aks holda indikator joyiga.
            if (TryGetPlanePose(touch.screenPosition, out Pose touchPose)) PlaceLab(touchPose);
            else if (hasPose) PlaceLab(indicatorPose);
        }

        private bool TryGetPlanePose(Vector2 screenPoint, out Pose pose)
        {
            pose = default;
            if (!_raycastManager.Raycast(screenPoint, s_arHits, placementTrackables)) return false;

            for (int i = 0; i < s_arHits.Count; i++)
            {
                ARPlane plane = _planeManager.GetPlane(s_arHits[i].trackableId);
                if (plane != null && plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    pose = s_arHits[i].pose;
                    return true;
                }
            }
            return false;
        }

        private void PlaceLab(Pose pose)
        {
            if (labRoot == null) return;

            Quaternion rotation = faceCameraOnPlace ? YawTowardsCamera(pose.position) : pose.rotation;

            if (_anchorObject != null) Destroy(_anchorObject);
            _anchorObject = new GameObject("CircuitLab_Anchor");
            _anchorObject.transform.SetPositionAndRotation(pose.position, rotation);
            _anchorObject.AddComponent<ARAnchor>(); // ARAnchorManager sahnada bo'lishi shart

            labRoot.SetParent(_anchorObject.transform, false);
            labRoot.localPosition = Vector3.zero;
            labRoot.localRotation = Quaternion.identity;
            labRoot.gameObject.SetActive(true);

            if (placementIndicator != null) placementIndicator.gameObject.SetActive(false);
            SetPlanesVisible(!hidePlanesAfterPlacement);
            _planeManager.enabled = false; // yangi tekisliklar qidiruvi to'xtatiladi (CPU/batareya tejaladi)

            SetState(State.Placed);
            LabPlaced?.Invoke(labRoot);
        }

        /// <summary>Stendni qayta joylashtirish rejimiga qaytaradi (UI tugmasi uchun).</summary>
        public void ResetPlacement()
        {
            _hover.Clear();
            _pressedCollider = null;
            _activeTouchId = -1;

            if (labRoot != null)
            {
                labRoot.SetParent(null, true);
                labRoot.gameObject.SetActive(false);
            }
            if (_anchorObject != null)
            {
                Destroy(_anchorObject);
                _anchorObject = null;
            }

            _planeManager.enabled = true;
            SetPlanesVisible(true);
            SetState(State.Scanning);
        }

        private void SetPlanesVisible(bool visible)
        {
            foreach (ARPlane plane in _planeManager.trackables) // TrackableCollection struct-enumerator — GC'siz
                plane.gameObject.SetActive(visible);
        }

        private Quaternion YawTowardsCamera(Vector3 position)
        {
            Vector3 toCamera = _cameraTransform.position - position;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude < 1e-6f) return Quaternion.identity;
            // Stendning "old" tomoni (−Z) foydalanuvchiga qaraydi → +Z kameradan uzoqqa.
            return Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
        }

        #endregion

        #region Interaction

        private void UpdateInteraction()
        {
            if (!TryGetTrackedTouch(out Touch touch))
            {
                if (_activeTouchId < 0 && _hover.HasTarget) _hover.Clear();
                return;
            }

            Collider hitCollider = RaycastFromScreen(touch.screenPosition);
            TouchPhase phase = touch.phase;

            switch (phase)
            {
                case TouchPhase.Began:
                    _pressedCollider = hitCollider;
                    _hover.UpdateTarget(hitCollider);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    _hover.UpdateTarget(hitCollider);
                    break;

                case TouchPhase.Ended:
                    _hover.UpdateTarget(hitCollider);
                    if (hitCollider != null && hitCollider == _pressedCollider) _hover.SelectCurrent();
                    EndTouch();
                    break;

                case TouchPhase.Canceled:
                    EndTouch();
                    break;
            }
        }

        private void EndTouch()
        {
            _pressedCollider = null;
            _activeTouchId = -1;
            _hover.Clear();
        }

        private Collider RaycastFromScreen(Vector2 screenPosition)
        {
            Ray ray = arCamera.ScreenPointToRay(screenPosition);
            return RaycastUtility.TryGetClosestHit(ray, _physicsHits, maxRayDistance, interactableMask, out RaycastHit hit)
                ? hit.collider
                : null;
        }

        /// <summary>Birinchi barmoqni kuzatadi (multi-touch e'tiborsiz).</summary>
        private bool TryGetTrackedTouch(out Touch tracked)
        {
            tracked = default;
            var touches = Touch.activeTouches; // ReadOnlyArray<Touch> — struct, GC'siz

            for (int i = 0; i < touches.Count; i++)
            {
                Touch t = touches[i];
                if (_activeTouchId < 0)
                {
                    if (t.phase != TouchPhase.Began || IsOverUI(t)) continue;
                    _activeTouchId = t.touchId;
                    tracked = t;
                    return true;
                }
                if (t.touchId == _activeTouchId)
                {
                    tracked = t;
                    return true;
                }
            }

            // Kuzatilayotgan barmoq ro'yxatdan chiqib ketgan (masalan, ilova fokusni yo'qotdi).
            if (_activeTouchId >= 0) EndTouch();
            return false;
        }

        private bool TryGetTouchBegan(out Touch began)
        {
            began = default;
            var touches = Touch.activeTouches;
            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i].phase == TouchPhase.Began && !IsOverUI(touches[i]))
                {
                    began = touches[i];
                    return true;
                }
            }
            return false;
        }

        private bool IsOverUI(Touch t)
        {
            return ignoreTouchesOverUI && EventSystem.current != null &&
                   EventSystem.current.IsPointerOverGameObject(t.touchId);
        }

        #endregion

        private void SetState(State next)
        {
            if (_state == next) return;
            _state = next;
            StateChanged?.Invoke(next);
        }
    }
}
