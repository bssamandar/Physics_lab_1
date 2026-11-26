using CircuitLab.Core;
using UnityEngine;

namespace CircuitLab.Visual
{
    /// <summary>
    /// Qisqa tutashuvda vizual va audio ogohlantirish: signal tovushi, miltillovchi World Space panel,
    /// batareya qutbida uchqunlar (oldindan yaratilgan ParticleSystem halqa-pool'i).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShortCircuitFeedback : MonoBehaviour
    {
        [SerializeField] private CircuitBoard board;

        [Header("Audio")]
        [Tooltip("Loop = true bo'lgan signal.")]
        [SerializeField] private AudioSource alarmSource;
        [SerializeField] private AudioSource sparkSource;

        [Header("UI")]
        [SerializeField] private CanvasGroup warningPanel;
        [SerializeField, Min(0.1f)] private float blinkFrequency = 3f;

        [Header("Sparks (pool)")]
        [SerializeField] private ParticleSystem sparkPrefab;
        [SerializeField, Range(1, 8)] private int sparkPoolSize = 3;
        [SerializeField, Min(0.05f)] private float sparkInterval = 0.6f;

        private ParticleSystem[] _sparks;
        private int _nextSpark;
        private float _sparkTimer;
        private bool _active;
        private CircuitComponent _source;

        private void Awake()
        {
            if (board == null) board = GetComponentInParent<CircuitBoard>();
            if (sparkPrefab != null)
            {
                _sparks = new ParticleSystem[sparkPoolSize];
                for (int i = 0; i < sparkPoolSize; i++)
                {
                    _sparks[i] = Instantiate(sparkPrefab, transform);
                    _sparks[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            SetWarningVisible(false);
        }

        private void OnEnable()
        {
            if (board != null) board.ShortCircuitChanged += HandleShortCircuit;
        }

        private void OnDisable()
        {
            if (board != null) board.ShortCircuitChanged -= HandleShortCircuit;
            HandleShortCircuit(false, null);
        }

        private void Update()
        {
            if (!_active) return;

            if (warningPanel != null)
                warningPanel.alpha = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI * blinkFrequency));

            _sparkTimer -= Time.deltaTime;
            if (_sparkTimer <= 0f)
            {
                _sparkTimer = sparkInterval;
                EmitSpark();
            }
        }

        private void HandleShortCircuit(bool isShort, CircuitComponent source)
        {
            _active = isShort;
            _source = source;
            _sparkTimer = 0f;

            if (alarmSource != null)
            {
                if (isShort && !alarmSource.isPlaying) alarmSource.Play();
                else if (!isShort && alarmSource.isPlaying) alarmSource.Stop();
            }
            SetWarningVisible(isShort);
        }

        private void EmitSpark()
        {
            if (_sparks == null || _source == null || _source.TerminalA == null) return;
            ParticleSystem ps = _sparks[_nextSpark];
            _nextSpark = (_nextSpark + 1) % _sparks.Length;

            ps.transform.position = _source.TerminalA.WirePoint;
            ps.Play(true);
            if (sparkSource != null) sparkSource.Play();
        }

        private void SetWarningVisible(bool visible)
        {
            if (warningPanel == null) return;
            warningPanel.gameObject.SetActive(visible);
            warningPanel.alpha = visible ? 1f : 0f;
        }
    }
}
