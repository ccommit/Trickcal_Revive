using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace TrickcalRevive.Recovery.Harness
{
    public enum CharacterResourceKind
    {
        Apostle,
        Monster
    }

    public enum CharacterPresentation
    {
        InGame,
        Standing
    }

    [Serializable]
    public sealed class CharacterResourceEntry
    {
        public string key;
        public string baseKey;
        public string displayName;
        public CharacterResourceKind kind;
        public Sprite portrait;
        public Sprite rosterIcon;
        public Sprite admissionSkillIcon;
        public Sprite graduateSkillIcon;
        public SkeletonDataAsset inGameSkeleton;
        public SkeletonDataAsset standingSkeleton;
        public string inGameSkin;
        public string standingSkin;
        public AudioClip[] battleVoices = Array.Empty<AudioClip>();
        public AudioClip[] battleSfx = Array.Empty<AudioClip>();
    }

    public sealed class CharacterResourceCatalog : ScriptableObject
    {
        [SerializeField] private List<CharacterResourceEntry> entries = new();

        public IReadOnlyList<CharacterResourceEntry> Entries => entries;

        public CharacterResourceEntry Find(string key)
        {
            return entries.Find(entry => string.Equals(entry.key, key, StringComparison.OrdinalIgnoreCase));
        }

        public void SetEntries(List<CharacterResourceEntry> value)
        {
            entries = value ?? new List<CharacterResourceEntry>();
        }

        public static AudioClip SelectDeterministic(
            IReadOnlyList<AudioClip> clips,
            int seed,
            int step)
        {
            if (clips == null || clips.Count == 0)
            {
                return null;
            }

            unchecked
            {
                var mixed = (uint)seed * 2654435761u + (uint)step * 2246822519u;
                return clips[(int)(mixed % (uint)clips.Count)];
            }
        }
    }
}
