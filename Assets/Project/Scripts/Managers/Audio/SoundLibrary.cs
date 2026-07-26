using System.Collections.Generic;
using UnityEngine;

namespace Managers.Audio
{
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [SerializeField] private List<SoundDefinition> _sounds = new();

        private Dictionary<SoundId, SoundDefinition> _lookup;

        public SoundDefinition Get(SoundId id)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<SoundId, SoundDefinition>();
                foreach (var sound in _sounds)
                {
                    if (sound != null) _lookup[sound.Id] = sound;
                }
            }

            _lookup.TryGetValue(id, out SoundDefinition definition);
            return definition;
        }
    }
}
