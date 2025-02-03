using UnityEngine;
using NaughtyAttributes;
namespace Kabir
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioPitchRandomizer : MonoBehaviour
    {
        [SerializeField] private bool _randomizeOnAwake = true;
        [SerializeField, MinMaxSlider(0f, 30f)]
        private Vector2 _pitchMultiplierRange = Vector2.one;
        void Start()
        {
            if(_randomizeOnAwake) RandomizePitch();
        }

        public void RandomizePitch() => RandomizePitch(_pitchMultiplierRange.x, _pitchMultiplierRange.y);

        public void RandomizePitch(float minPitchMultiplier, float maxPitchMultiplier)
        {
            float min = Mathf.Max(minPitchMultiplier, 0f);
            float max = Mathf.Clamp(maxPitchMultiplier, min, 30f);

            AudioSource source = GetComponent<AudioSource>();
            source.pitch = Random.Range(min, max) * source.pitch;
        }
    }
}
