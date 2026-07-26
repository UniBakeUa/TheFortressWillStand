using UnityEngine;

namespace Managers.Audio
{
    [System.Serializable]
    public class SoundDefinition
    {
        public SoundId Id;
        public AudioClip[] Clips;

        [Range(0f, 1f)] public float Volume = 1f;
        [Tooltip("Випадковий розкид висоти тону навколо 1.0 (0 = вимкнено)")]
        [Range(0f, 0.5f)] public float PitchVariance = 0f;

        [Header("3D Distance")]
        public float MinDistance = 3f;
        public float MaxDistance = 25f;

        public AudioClip GetRandomClip()
        {
            if (Clips == null || Clips.Length == 0) return null;
            return Clips[Random.Range(0, Clips.Length)];
        }
    }
}
