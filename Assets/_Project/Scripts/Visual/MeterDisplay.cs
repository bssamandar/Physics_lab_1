using CircuitLab.Core;
using TMPro;
using UnityEngine;

namespace CircuitLab.Visual
{
    /// <summary>
    /// Raqamli Ampermetr/Voltmetr ekrani (World Space Canvas + TextMeshPro).
    /// Matn faqat qiymat o'zgarganda, GC'siz <see cref="TMP_Text.SetText(string, float)"/> orqali yangilanadi.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircuitComponent))]
    public sealed class MeterDisplay : MonoBehaviour
    {
        /// <summary>Ko'rsatiladigan kattalik.</summary>
        public enum Quantity
        {
            /// <summary>Komponent turiga qarab (Ammeter → tok, qolganlari → kuchlanish).</summary>
            Auto = 0,
            /// <summary>Tok, A.</summary>
            Current = 1,
            /// <summary>Kuchlanish, V.</summary>
            Voltage = 2
        }

        [SerializeField] private TMP_Text label;
        [SerializeField] private Quantity quantity = Quantity.Auto;
        [Tooltip("TMP SetText formati: {0:2} — 2 kasr xonasi.")]
        [SerializeField] private string currentFormat = "{0:3} A";
        [SerializeField] private string voltageFormat = "{0:2} V";
        [SerializeField] private string overloadText = "OL";
        [SerializeField] private Color normalColor = new Color(0.3f, 1f, 0.45f, 1f);
        [SerializeField] private Color overloadColor = new Color(1f, 0.2f, 0.2f, 1f);
        [Tooltip("Shundan kichik o'zgarishlar matnni yangilamaydi.")]
        [SerializeField, Min(0f)] private float displayEpsilon = 0.0005f;

        [Header("Billboard (ixtiyoriy)")]
        [SerializeField] private Transform billboardRoot;
        [SerializeField] private Transform viewer;

        private CircuitComponent _component;
        private float _shownValue = float.NaN;
        private bool _shownOverload;

        private void Awake()
        {
            _component = GetComponent<CircuitComponent>();
        }

        private void Start()
        {
            if (billboardRoot != null && viewer == null && Camera.main != null) viewer = Camera.main.transform;
        }

        private void OnEnable()
        {
            _component.StateChanged += HandleStateChanged;
            _shownValue = float.NaN;
            HandleStateChanged(_component);
        }

        private void OnDisable()
        {
            _component.StateChanged -= HandleStateChanged;
        }

        private void LateUpdate()
        {
            if (billboardRoot == null || viewer == null) return;
            Vector3 toViewer = billboardRoot.position - viewer.position;
            if (toViewer.sqrMagnitude > 1e-6f) billboardRoot.rotation = Quaternion.LookRotation(toViewer, Vector3.up);
        }

        private void HandleStateChanged(CircuitComponent c)
        {
            if (label == null) return;

            bool showCurrent = quantity == Quantity.Current ||
                               (quantity == Quantity.Auto && c.Kind == ComponentKind.Ammeter);
            bool overload = c.IsOverloaded;

            if (overload)
            {
                if (_shownOverload) return;
                _shownOverload = true;
                _shownValue = float.NaN;
                label.SetText(overloadText);
                label.color = overloadColor;
                return;
            }

            float value = showCurrent ? c.Current : c.VoltageDrop;
            if (!_shownOverload && !float.IsNaN(_shownValue) && Mathf.Abs(value - _shownValue) < displayEpsilon) return;

            if (_shownOverload) label.color = normalColor;
            _shownOverload = false;
            _shownValue = value;
            label.SetText(showCurrent ? currentFormat : voltageFormat, value);
        }
    }
}
