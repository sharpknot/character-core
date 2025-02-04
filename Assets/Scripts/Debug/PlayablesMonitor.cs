using Kabir.CharacterComponents;
using TMPro;
using UnityEngine;

namespace Kabir.DebugTools
{
    public class PlayablesMonitor : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private PlayableManager _playableManager;
        void Update()
        {
            if (_playableManager == null || _text == null) return;

            string str = $"PlayableManager ({_playableManager.gameObject.name}): ";

            int orphanCount = _playableManager.PlayableGraph.GetRootPlayableCount() ;

            str += $"{orphanCount} root playables,";

            _text.text = str ;
        }
    }
}
