using System.Collections.Generic;
using CircuitLab.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace CircuitLab.Visual
{
    /// <summary>
    /// Simlarni kvadratik Bezye egri chizig'i bo'yicha salqiydigan 3D chiziq sifatida chizadi.
    /// <see cref="LineRenderer"/>lar <see cref="ObjectPool{T}"/> orqali qayta ishlatiladi.
    /// Tok yo'nalishi va kuchi tekstura siljishi (UV scroll) hamda rang bilan ko'rsatiladi.
    /// </summary>
    /// <remarks>
    /// GC: nuqtalar massivi har bir pool elementi uchun bir marta ajratiladi;
    /// LateUpdate faqat <see cref="LineRenderer.SetPositions"/> va <see cref="MaterialPropertyBlock"/> dan foydalanadi.
    /// Sim prefabi: LineRenderer, Texture Mode = Tile, URP/Unlit (yoki Particles/Unlit) material, chiziqli (arrow/dash) tekstura.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class WireVisualizer : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

        [Header("Dependencies")]
        [SerializeField] private CircuitBoard board;
        [SerializeField] private LineRenderer wirePrefab;

        [Header("Shape")]
        [SerializeField, Range(4, 48)] private int segments = 20;
        [Tooltip("Har metr uzunlikka salqilik (m).")]
        [SerializeField, Min(0f)] private float sagPerMeter = 0.35f;
        [SerializeField, Min(0f)] private float maxSag = 0.12f;
        [Tooltip("Sim stend sirtidan pastga tushmasligi uchun minimal balandlik (m).")]
        [SerializeField, Min(0f)] private float minHeightAboveBoard = 0.004f;
        [SerializeField, Min(0.0005f)] private float width = 0.006f;

        [Header("Current feedback")]
        [SerializeField] private Color idleColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color activeColor = new Color(1f, 0.55f, 0.1f, 1f);
        [SerializeField] private Color shortCircuitColor = new Color(1f, 0.05f, 0.05f, 1f);
        [Tooltip("Shu tokda rang to'liq activeColor bo'ladi (A).")]
        [SerializeField, Min(0.001f)] private float fullColorCurrent = 0.5f;
        [Tooltip("1 A tokda tekstura siljish tezligi (UV/s).")]
        [SerializeField] private float scrollSpeedPerAmp = 2.5f;
        [SerializeField, Min(0f)] private float maxScrollSpeed = 6f;
        [SerializeField, Min(0f)] private float minVisibleCurrent = 0.001f;
        [Tooltip("Tekstura takrorlanishi: Tile rejimida UV.x dunyo birligida, shuning uchun bu — metrga takrorlanish soni.")]
        [SerializeField, Min(0.01f)] private float textureTilesPerMeter = 40f;

        [Header("Pool")]
        [SerializeField, Min(1)] private int prewarmCount = 12;

        private sealed class WireView
        {
            public LineRenderer Line;
            public Vector3[] Points;
            public WireConnection Connection;
            public Vector3 LastA;
            public Vector3 LastB;
            public float UvOffset;
        }

        private readonly List<WireView> _active = new List<WireView>(32);
        private ObjectPool<WireView> _pool;
        private MaterialPropertyBlock _mpb;
        private bool _shortCircuit;

        private void Awake()
        {
            if (board == null) board = GetComponentInParent<CircuitBoard>();
            _mpb = new MaterialPropertyBlock();
            _pool = new ObjectPool<WireView>(CreateView, OnGetView, OnReleaseView, OnDestroyView,
                collectionCheck: false, defaultCapacity: prewarmCount, maxSize: Mathf.Max(prewarmCount, board != null ? board.MaxWires : 64));
            Prewarm();
        }

        private void OnEnable()
        {
            if (board == null) return;
            board.WireAdded += HandleWireAdded;
            board.WireRemoved += HandleWireRemoved;
            board.ShortCircuitChanged += HandleShortCircuitChanged;

            IReadOnlyList<WireConnection> wires = board.Wires;
            for (int i = 0; i < wires.Count; i++) HandleWireAdded(wires[i]);
        }

        private void OnDisable()
        {
            if (board == null) return;
            board.WireAdded -= HandleWireAdded;
            board.WireRemoved -= HandleWireRemoved;
            board.ShortCircuitChanged -= HandleShortCircuitChanged;
            for (int i = _active.Count - 1; i >= 0; i--) _pool.Release(_active[i]);
            _active.Clear();
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _active.Count; i++)
            {
                WireView view = _active[i];
                Vector3 a = view.Connection.TerminalA.WirePoint;
                Vector3 b = view.Connection.TerminalB.WirePoint;

                if ((a - view.LastA).sqrMagnitude > 1e-8f || (b - view.LastB).sqrMagnitude > 1e-8f)
                {
                    RebuildCurve(view, a, b);
                }

                ApplyCurrentFeedback(view, dt);
            }
        }

        #region Pool

        private void Prewarm()
        {
            int n = prewarmCount;
            WireView[] temp = new WireView[n]; // faqat Awake'da, bir marta
            for (int i = 0; i < n; i++) temp[i] = _pool.Get();
            for (int i = 0; i < n; i++) _pool.Release(temp[i]);
        }

        private WireView CreateView()
        {
            LineRenderer line = Instantiate(wirePrefab, transform);
            line.useWorldSpace = true;
            line.positionCount = segments + 1;
            line.widthMultiplier = width;
            line.textureMode = LineTextureMode.Tile;
            line.gameObject.SetActive(false);
            return new WireView { Line = line, Points = new Vector3[segments + 1] };
        }

        private static void OnGetView(WireView view)
        {
            view.Line.gameObject.SetActive(true);
        }

        private static void OnReleaseView(WireView view)
        {
            view.Connection = null;
            view.UvOffset = 0f;
            if (view.Line != null) view.Line.gameObject.SetActive(false);
        }

        private static void OnDestroyView(WireView view)
        {
            if (view.Line != null) Destroy(view.Line.gameObject);
        }

        #endregion

        #region Event handlers

        private void HandleWireAdded(WireConnection connection)
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].Connection == connection) return;

            WireView view = _pool.Get();
            view.Connection = connection;
            RebuildCurve(view, connection.TerminalA.WirePoint, connection.TerminalB.WirePoint);
            ApplyCurrentFeedback(view, 0f);
            _active.Add(view);
        }

        private void HandleWireRemoved(WireConnection connection)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Connection != connection) continue;
                WireView view = _active[i];
                int last = _active.Count - 1;
                _active[i] = _active[last];
                _active.RemoveAt(last);
                _pool.Release(view);
                return;
            }
        }

        private void HandleShortCircuitChanged(bool isShort, CircuitComponent source)
        {
            _shortCircuit = isShort;
        }

        #endregion

        #region Curve & feedback

        /// <summary>B(t) = (1−t)²·P0 + 2(1−t)t·P1 + t²·P2, P1 — o'rta nuqtadan pastga surilgan nazorat nuqtasi.</summary>
        private void RebuildCurve(WireView view, Vector3 a, Vector3 b)
        {
            view.LastA = a;
            view.LastB = b;

            Vector3 up = board != null ? board.transform.up : Vector3.up;
            float length = Vector3.Distance(a, b);
            float sag = Mathf.Min(length * sagPerMeter, maxSag);

            if (board != null)
            {
                Vector3 origin = board.transform.position;
                float heightA = Vector3.Dot(a - origin, up);
                float heightB = Vector3.Dot(b - origin, up);
                // Egri chiziqning eng past nuqtasi ≈ min(h) − sag/2 → stenddan pastga tushmasin.
                float allowed = Mathf.Max(0f, 2f * (Mathf.Min(heightA, heightB) - minHeightAboveBoard));
                sag = Mathf.Min(sag, allowed);
            }

            Vector3 control = (a + b) * 0.5f - up * sag;
            Vector3[] pts = view.Points;
            int last = pts.Length - 1;
            float inv = 1f / last;
            for (int i = 0; i <= last; i++)
            {
                float t = i * inv;
                float u = 1f - t;
                pts[i] = (u * u) * a + (2f * u * t) * control + (t * t) * b;
            }

            view.Line.SetPositions(pts);
        }

        private void ApplyCurrentFeedback(WireView view, float dt)
        {
            float current = view.Connection.Current;
            float magnitude = Mathf.Abs(current);

            if (magnitude > minVisibleCurrent)
            {
                float speed = Mathf.Clamp(current * scrollSpeedPerAmp, -maxScrollSpeed, maxScrollSpeed);
                view.UvOffset = Mathf.Repeat(view.UvOffset - speed * dt, 1f);
            }

            Color color = _shortCircuit
                ? shortCircuitColor
                : Color.Lerp(idleColor, activeColor, Mathf.Clamp01(magnitude / fullColorCurrent));

            view.Line.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, color);
            _mpb.SetVector(BaseMapStId, new Vector4(textureTilesPerMeter, 1f, view.UvOffset, 0f));
            view.Line.SetPropertyBlock(_mpb);
        }

        #endregion
    }
}
