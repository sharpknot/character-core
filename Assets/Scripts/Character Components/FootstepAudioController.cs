using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using DG.Tweening;

namespace Kabir.CharacterComponents
{
    public class FootstepAudioController : MonoBehaviour
    {
        [SerializeField, Required] private AnimationEventManager _eventManager;
        [SerializeField] private AudioSource[] _defaultFootstepAudio;
        [SerializeField] private float _minInterval = 0.5f;

        private Sequence _intervalSequence;

        private AudioSource[] _defaultAudios;
        void Start()
        {
            _defaultAudios??=GetDefaultAudioSources();
            if (_eventManager != null) _eventManager.OnFootstep += PlayFootstepAudio;
        }

        private void OnDestroy()
        {
            if (_eventManager != null) _eventManager.OnFootstep -= PlayFootstepAudio;
            KillSequence();
        }

        private void OnValidate()
        {
            _minInterval = Mathf.Max(0f, _minInterval);
        }

        private void PlayFootstepAudio(int index)
        {
            if (_intervalSequence != null) return;
            _defaultAudios ??= GetDefaultAudioSources();
            if (_defaultAudios.Length <= 0) return;

            AudioSource source = _defaultAudios[Random.Range(0, _defaultAudios.Length)];    
            Instantiate(source.gameObject, transform.position, transform.rotation);

            if(_minInterval > 0)
            {
                _intervalSequence = DOTween.Sequence().AppendInterval(_minInterval).AppendCallback(KillSequence);
            }
        }

        private void KillSequence()
        {
            if (_intervalSequence != null)
            {
                _intervalSequence.Kill();
                _intervalSequence = null;
            }
        }

        private AudioSource[] GetDefaultAudioSources()
        {
            List<AudioSource> result = new();
            if (_defaultFootstepAudio == null) return result.ToArray();
            
            foreach(var audioSource in _defaultFootstepAudio)
            {
                if (audioSource == null) continue;
                if(result.Contains(audioSource)) continue;
                result.Add(audioSource);
            }

            return result.ToArray();
        }
    }
}
