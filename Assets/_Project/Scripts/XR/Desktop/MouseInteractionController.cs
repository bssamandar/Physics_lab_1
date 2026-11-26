using CircuitLab.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CircuitLab.XR.Desktop
{
    /// <summary>
    /// Editor/Desktop'da qurilmasiz sinash uchun sichqoncha kontrolleri.
    /// Xuddi AR/VR kontrollerlari kabi faqat <see cref="InteractableHoverTracker"/> va
    /// <see cref="CircuitLab.Core.Interaction.ICircuitInteractable"/> bilan ishlaydi (Open/Closed tamoyili).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MouseInteractionController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask interactableMask = 1 << 8;
        [SerializeField, Min(0.1f)] private float maxDistance = 20f;

        private readonly RaycastHit[] _hits = new RaycastHit[8];
        private readonly InteractableHoverTracker _hover = new InteractableHoverTracker();

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void OnDisable()
        {
            _hover.Clear();
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || targetCamera == null) return;

            Ray ray = targetCamera.ScreenPointToRay(mouse.position.ReadValue());
            _hover.UpdateTarget(RaycastUtility.TryGetClosestHit(ray, _hits, maxDistance, interactableMask, out RaycastHit hit)
                ? hit.collider
                : null);

            if (mouse.leftButton.wasPressedThisFrame) _hover.SelectCurrent();
        }
    }
}
