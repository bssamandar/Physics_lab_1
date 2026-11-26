using CircuitLab.Core;
using UnityEngine;

namespace CircuitLab.Visual
{
    /// <summary>
    /// Terminal holatini (Idle/Hovered/Pending) rang va masshtab orqali ko'rsatadi.
    /// Core qatlamidagi <see cref="CircuitTerminal.VisualStateChanged"/> hodisasiga obuna bo'ladi.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircuitTerminal))]
    public sealed class TerminalHighlighter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color idleColor = new Color(0.75f, 0.6f, 0.2f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.3f, 0.85f, 1f, 1f);
        [SerializeField] private Color pendingColor = new Color(0.2f, 1f, 0.35f, 1f);
        [SerializeField, Min(1f)] private float hoverScale = 1.25f;
        [SerializeField, Min(0.1f)] private float scaleSpeed = 14f;

        private CircuitTerminal _terminal;
        private MaterialPropertyBlock _mpb;
        private Vector3 _baseScale;
        private float _targetScale = 1f;
        private float _scale = 1f;

        private void Awake()
        {
            _terminal = GetComponent<CircuitTerminal>();
            _mpb = new MaterialPropertyBlock();
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
            _baseScale = targetRenderer != null ? targetRenderer.transform.localScale : Vector3.one;
        }

        private void OnEnable()
        {
            _terminal.VisualStateChanged += HandleState;
            HandleState(_terminal, _terminal.VisualState);
        }

        private void OnDisable()
        {
            _terminal.VisualStateChanged -= HandleState;
        }

        private void Update()
        {
            if (targetRenderer == null || Mathf.Approximately(_scale, _targetScale)) return;
            _scale = Mathf.MoveTowards(_scale, _targetScale, scaleSpeed * Time.deltaTime);
            targetRenderer.transform.localScale = _baseScale * _scale;
        }

        private void HandleState(CircuitTerminal terminal, TerminalVisualState state)
        {
            if (targetRenderer == null) return;
            Color c = state == TerminalVisualState.Pending ? pendingColor
                : state == TerminalVisualState.Hovered ? hoverColor
                : idleColor;
            _targetScale = state == TerminalVisualState.Idle ? 1f : hoverScale;

            targetRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            targetRenderer.SetPropertyBlock(_mpb);
        }
    }
}
