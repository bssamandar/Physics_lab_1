using CircuitLab.Core;
using UnityEngine;

namespace CircuitLab.Visual
{
    /// <summary>
    /// Lampochka yorqinligi: quvvatga (P = I²·R) proporsional Emission va Point Light intensivligi.
    /// Ma'lumot faqat <see cref="CircuitComponent.StateChanged"/> hodisasi orqali olinadi.
    /// </summary>
    /// <remarks>
    /// Filament materiali URP/Lit bo'lishi, "Emission" yoqilgan bo'lishi shart (_EMISSION keyword).
    /// Rang <see cref="MaterialPropertyBlock"/> orqali beriladi — material nusxalanmaydi.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircuitComponent))]
    public sealed class BulbVisualizer : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer filamentRenderer;
        [SerializeField] private Light pointLight;
        [Tooltip("Kuyganda yoqiladigan obyekt (qoraygan shisha, tutun va h.k.).")]
        [SerializeField] private GameObject brokenVisual;

        [SerializeField, ColorUsage(false, true)] private Color emissionColor = new Color(1f, 0.78f, 0.45f, 1f);
        [SerializeField, Min(0f)] private float maxEmissionIntensity = 6f;
        [SerializeField, Min(0f)] private float maxLightIntensity = 1.5f;
        [Tooltip("Yorqinlik o'zgarish tezligi (filament inersiyasi).")]
        [SerializeField, Min(0.1f)] private float responseSpeed = 12f;
        [Tooltip("Perseptiv egri: 0.5 — kvadrat ildiz.")]
        [SerializeField, Range(0.2f, 1f)] private float gamma = 0.5f;

        private CircuitComponent _component;
        private MaterialPropertyBlock _mpb;
        private float _target;
        private float _current = -1f;

        /// <summary>Joriy normallashtirilgan yorqinlik [0..1].</summary>
        public float Brightness => Mathf.Max(0f, _current);

        private void Awake()
        {
            _component = GetComponent<CircuitComponent>();
            _mpb = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _component.StateChanged += HandleStateChanged;
            _component.ConfigurationChanged += HandleStateChanged;
            HandleStateChanged(_component);
        }

        private void OnDisable()
        {
            _component.StateChanged -= HandleStateChanged;
            _component.ConfigurationChanged -= HandleStateChanged;
        }

        private void Update()
        {
            if (Mathf.Abs(_current - _target) < 0.001f) return;
            _current = _current < 0f ? _target : Mathf.MoveTowards(_current, _target, responseSpeed * Time.deltaTime);
            Apply(_current);
        }

        private void HandleStateChanged(CircuitComponent c)
        {
            ComponentDataSO data = c.Data;
            if (data == null || c.IsBroken)
            {
                _target = 0f;
            }
            else
            {
                float power = c.Current * c.Current * data.Resistance;
                _target = Mathf.Pow(Mathf.Clamp01(power / data.RatedPower), gamma);
            }

            if (brokenVisual != null && brokenVisual.activeSelf != c.IsBroken) brokenVisual.SetActive(c.IsBroken);
        }

        private void Apply(float brightness)
        {
            if (filamentRenderer != null)
            {
                filamentRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionColorId, emissionColor * (brightness * maxEmissionIntensity));
                filamentRenderer.SetPropertyBlock(_mpb);
            }

            if (pointLight != null)
            {
                bool on = brightness > 0.01f;
                if (pointLight.enabled != on) pointLight.enabled = on;
                pointLight.intensity = brightness * maxLightIntensity;
            }
        }
    }
}
