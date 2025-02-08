using NaughtyAttributes;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Kabir
{
    public class CountdownController : MonoBehaviour
    {
        public float Duration
        {
            get { return _duration; }
            set { _duration = Mathf.Max(value, 0f); }
        }

        [SerializeField]
        private float _duration = 0f;
        public float RemainingDuration => _remainingDuration;
        [SerializeField, ReadOnly]
        private float _remainingDuration = 0f;

        public event UnityAction OnComplete;
        public UnityEvent OnCompleteEvent;

        [SerializeField]
        private bool _runOnStart;

        private IEnumerator _progress;
        private IEnumerator CountdownProgress(float duration)
        {
            _duration = Mathf.Max(duration, 0f);
            _remainingDuration = _duration;

            while (_remainingDuration > 0f)
            {
                yield return null;
                _remainingDuration -= Time.deltaTime;
            }

            _remainingDuration = 0f;
            OnCompleteEvents();
            KillProgress();
        }

        void Start()
        {
            if (_runOnStart) StartCountdown();
        }

        private void OnDestroy()
        {
            KillProgress();
        }

        public void StopCountdown()
        {
            KillProgress();
            _remainingDuration = 0f;
        }

        public void StartCountdown() => StartCountdown(_duration);

        public void StartCountdown(float duration)
        {
            StopCountdown();
            _progress = CountdownProgress(duration);
            StartCoroutine(_progress);
        }

        private void OnValidate()
        {
            _duration = Mathf.Max(_duration, 0f);
        }

        private void OnCompleteEvents()
        {
            OnComplete?.Invoke();
            OnCompleteEvent?.Invoke();
        }

        private void KillProgress()
        {
            if (_progress == null) return;
            StopCoroutine(_progress);
            _progress = null;
        }
    }
}
