using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;

namespace Kabir
{
    public class SelfDestroy : MonoBehaviour
    {
        [SerializeField] private bool _runOnAwake;
        [SerializeField] private float _countdown;
        private Sequence _sequence;

        void Start()
        {
            KillSequence();
            if(_runOnAwake) DestroyGameObject();
        }

        private void OnDestroy()
        {
            KillSequence();
        }

        private void OnValidate()
        {
            _countdown = Mathf.Max(0f, _countdown);
        }

        public void DestroyGameObject() => DestroyGameObject(_countdown);

        public void DestroyGameObject(float customTimerSeconds)
        {
            KillSequence();
            float timer = Mathf.Max(customTimerSeconds, 0f);
            if (timer > 0f)
            {
                _sequence = DOTween.Sequence().AppendInterval(timer).AppendCallback(CompleteDestroy);
                return;
            }

            CompleteDestroy();
        }

        private void CompleteDestroy()
        {
            KillSequence();
            Destroy(gameObject);
        }

        private void KillSequence()
        {
            if (_sequence == null) return;
            _sequence.Kill();
            _sequence = null;
        }
    }
}
