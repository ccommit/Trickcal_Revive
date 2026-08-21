using System;
using System.Linq;
using Spine.Unity;
using UnityEngine;

namespace TrickcalRevive.Recovery.Harness
{
    public sealed class CharacterResourceHarness : MonoBehaviour
    {
        [SerializeField] private CharacterResourceCatalog catalog;
        [SerializeField] private string entryKey = "amelia";
        [SerializeField] private CharacterPresentation presentation = CharacterPresentation.InGame;

        private SkeletonAnimation activeSkeleton;

        public CharacterResourceCatalog Catalog => catalog;
        public SkeletonAnimation ActiveSkeleton => activeSkeleton;
        public bool IsReady => activeSkeleton != null
                               && activeSkeleton.valid
                               && activeSkeleton.Skeleton != null
                               && activeSkeleton.GetComponent<MeshFilter>()?.sharedMesh != null
                               && activeSkeleton.GetComponent<MeshFilter>().sharedMesh.vertexCount > 0;

        private void Start()
        {
            ApplySelection();
        }

        public void Configure(
            CharacterResourceCatalog value,
            string key,
            CharacterPresentation selectedPresentation,
            bool initialize = false)
        {
            catalog = value;
            entryKey = key;
            presentation = selectedPresentation;
            if (initialize)
            {
                ApplySelection();
            }
        }

        public void Select(string key, CharacterPresentation selectedPresentation)
        {
            entryKey = key;
            presentation = selectedPresentation;
            ApplySelection();
        }

        public AudioClip SelectBattleVoice(int seed, int step)
        {
            var entry = catalog != null ? catalog.Find(entryKey) : null;
            return CharacterResourceCatalog.SelectDeterministic(entry?.battleVoices, seed, step);
        }

        public AudioClip SelectBattleSfx(int seed, int step)
        {
            var entry = catalog != null ? catalog.Find(entryKey) : null;
            return CharacterResourceCatalog.SelectDeterministic(entry?.battleSfx, seed, step);
        }

        public void ApplySelection()
        {
            DestroyActiveSkeleton();
            var entry = catalog != null ? catalog.Find(entryKey) : null;
            if (entry == null)
            {
                return;
            }

            var skeletonAsset = presentation == CharacterPresentation.Standing
                ? entry.standingSkeleton
                : entry.inGameSkeleton;
            if (skeletonAsset == null)
            {
                return;
            }

            activeSkeleton = SkeletonAnimation.NewSkeletonAnimationGameObject(skeletonAsset);
            activeSkeleton.name = $"{entry.key}-{presentation}";
            activeSkeleton.transform.SetParent(transform, false);

            var skinName = presentation == CharacterPresentation.Standing
                ? entry.standingSkin
                : entry.inGameSkin;
            if (!string.IsNullOrWhiteSpace(skinName)
                && activeSkeleton.Skeleton.Data.FindSkin(skinName) != null)
            {
                activeSkeleton.Skeleton.SetSkin(skinName);
                activeSkeleton.Skeleton.SetSlotsToSetupPose();
            }

            var animation = activeSkeleton.Skeleton.Data.Animations
                .Take(activeSkeleton.Skeleton.Data.Animations.Count)
                .OrderBy(item => item.Name, StringComparer.Ordinal)
                .FirstOrDefault(item => item.Name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? activeSkeleton.Skeleton.Data.Animations
                    .Take(activeSkeleton.Skeleton.Data.Animations.Count)
                    .OrderBy(item => item.Name, StringComparer.Ordinal)
                    .FirstOrDefault();
            if (animation != null)
            {
                activeSkeleton.AnimationState.SetAnimation(0, animation.Name, true);
            }

            activeSkeleton.Update(0f);
            activeSkeleton.LateUpdate();
        }

        private void DestroyActiveSkeleton()
        {
            if (activeSkeleton == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(activeSkeleton.gameObject);
            }
            else
            {
                DestroyImmediate(activeSkeleton.gameObject);
            }
            activeSkeleton = null;
        }
    }
}
