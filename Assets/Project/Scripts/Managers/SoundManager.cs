using System.Collections.Generic;
using Managers.Audio;
using UnityEngine;

namespace Managers
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance;

        [SerializeField] private SoundLibrary _library;
        [SerializeField] private int _poolSize = 24;
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField] private int _maxConcurrentPerSound = 3;

        [Header("3D Sound")]
        [SerializeField, Range(0f, 1f)] private float _spatialBlend = 0.5f;
        [SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Linear;

        private readonly List<AudioSource> _pool = new();
        private readonly Dictionary<AudioSource, SoundId> _sourceSounds = new();

        private void Awake()
        {
            Instance = this;

            for (int i = 0; i < _poolSize; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = _spatialBlend;
                source.rolloffMode = _rolloffMode;
                _pool.Add(source);
            }
        }

        public void Play(SoundId id, Vector3 position)
        {
            SoundDefinition definition = _library != null ? _library.Get(id) : null;
            if (definition == null) return;

            AudioClip clip = definition.GetRandomClip();
            if (clip == null) return;

            if (CountPlaying(id) >= _maxConcurrentPerSound) return;

            AudioSource source = GetFreeSource();
            if (source == null) return;

            source.transform.position = position;
            source.volume = definition.Volume * _masterVolume;
            source.pitch = 1f + Random.Range(-definition.PitchVariance, definition.PitchVariance);
            source.minDistance = definition.MinDistance;
            source.maxDistance = definition.MaxDistance;
            source.clip = clip;
            source.Play();

            _sourceSounds[source] = id;
        }

        private int CountPlaying(SoundId id)
        {
            int count = 0;
            foreach (var source in _pool)
            {
                if (source.isPlaying && _sourceSounds.TryGetValue(source, out var playingId) && playingId == id)
                {
                    count++;
                }
            }
            return count;
        }

        private AudioSource GetFreeSource()
        {
            foreach (var source in _pool)
            {
                if (!source.isPlaying) return source;
            }

            return null;
        }
    }
}
