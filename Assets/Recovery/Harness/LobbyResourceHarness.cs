using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrickcalRevive.Recovery.Harness
{
    public sealed class LobbyResourceHarness : MonoBehaviour
    {
        [SerializeField]
        private Sprite background;

        [SerializeField]
        private Sprite[] approvedSprites = Array.Empty<Sprite>();

        public Sprite Background => background;

        public IReadOnlyList<Sprite> ApprovedSprites => approvedSprites;

        public bool IsReady
        {
            get
            {
                if (background == null || approvedSprites == null || approvedSprites.Length != 14)
                {
                    return false;
                }

                foreach (var sprite in approvedSprites)
                {
                    if (sprite == null || sprite.texture == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void Configure(Sprite recoveredBackground, Sprite[] recoveredSprites)
        {
            if (recoveredBackground == null)
            {
                throw new ArgumentNullException(nameof(recoveredBackground));
            }
            if (recoveredSprites == null || recoveredSprites.Length != 14)
            {
                throw new ArgumentException("The lobby harness requires exactly 14 UI sprites.", nameof(recoveredSprites));
            }

            background = recoveredBackground;
            approvedSprites = (Sprite[])recoveredSprites.Clone();
        }
    }
}
