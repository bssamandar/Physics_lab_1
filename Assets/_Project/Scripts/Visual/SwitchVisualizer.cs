using CircuitLab.Core;
using UnityEngine;

namespace CircuitLab.Visual
{
    /// <summary>Kalit richagini ochiq/yopiq burchakka silliq aylantiradi.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircuitComponent))]
    public sealed class SwitchVisualizer : MonoBehaviour
    {
        [SerializeField] private Transform lever;
        [SerializeField] private Vector3 openEuler = new Vector3(0f, 0f, 35f);
        [SerializeField] private Vector3 closedEuler = Vector3.zero;
        [SerializeField, Min(1f)] private float rotateSpeed = 540f;
        [SerializeField] private AudioSource clickAudio;

        private CircuitComponent _component;
        private Quaternion _open;
        private Quaternion _closed;
        private bool _lastClosed;

        private void Awake()
        {
            _component = GetComponent<CircuitComponent>();
            _open = Quaternion.Euler(openEuler);
            _closed = Quaternion.Euler(closedEuler);
        }

        private void OnEnable()
        {
            _component.ConfigurationChanged += HandleConfig;
            _lastClosed = _component.IsClosed;
            if (lever != null) lever.localRotation = _lastClosed ? _closed : _open;
        }

        private void OnDisable()
        {
            _component.ConfigurationChanged -= HandleConfig;
        }

        private void Update()
        {
            if (lever == null) return;
            Quaternion target = _component.IsClosed ? _closed : _open;
            if (lever.localRotation != target)
                lever.localRotation = Quaternion.RotateTowards(lever.localRotation, target, rotateSpeed * Time.deltaTime);
        }

        private void HandleConfig(CircuitComponent c)
        {
            if (c.IsClosed == _lastClosed) return;
            _lastClosed = c.IsClosed;
            if (clickAudio != null) clickAudio.Play();
        }
    }
}
