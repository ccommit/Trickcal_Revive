using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TrickcalRevive.App.Editor
{
    /// <summary>
    /// Rebuilds the small set of TMP material presets evidenced by the restored screens.
    /// The generated materials share the current TMP font atlas; no extracted texture is copied.
    /// </summary>
    public static class RecoveredTextStyles
    {
        public const string WhiteBorderMaterialPath =
            "Assets/Game/Content/UI/Fonts/Materials/ONE Mobile POP SDF - Recovered White Border.mat";
        public const string BlackBorderMaterialPath =
            "Assets/Game/Content/UI/Fonts/Materials/ONE Mobile POP SDF - Recovered Black Border.mat";
        public const string BattleListMaterialPath =
            "Assets/Game/Content/UI/Fonts/Materials/ONE Mobile POP SDF - Recovered Battle List.mat";

        private static readonly Color32 DarkFace = new Color32(38, 32, 29, 255);
        // The stage-number capture shows a white face with a charcoal border.
        private static readonly Color StageNumberBorder = new Color32(38, 32, 29, 255);

        public static void Apply(TMP_Text text, RecoveredTextStyle style)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (text.font == null)
                throw new InvalidOperationException($"Text '{text.name}' has no TMP font asset.");

            switch (style)
            {
                case RecoveredTextStyle.DarkWithLightBorder:
                    text.color = DarkFace;
                    text.fontSharedMaterial = EnsureMaterial(
                        text.font,
                        WhiteBorderMaterialPath,
                        "ONE Mobile POP SDF - Recovered White Border",
                        Color.white);
                    break;
                case RecoveredTextStyle.LightWithDarkBorder:
                    text.color = Color.white;
                    text.fontSharedMaterial = EnsureMaterial(
                        text.font,
                        BlackBorderMaterialPath,
                        "ONE Mobile POP SDF - Recovered Black Border",
                        Color.black);
                    break;
                case RecoveredTextStyle.StageNumber:
                    text.color = Color.white;
                    text.fontSharedMaterial = EnsureMaterial(
                        text.font,
                        BattleListMaterialPath,
                        "ONE Mobile POP SDF - Recovered Battle List",
                        StageNumberBorder);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, null);
            }

            EditorUtility.SetDirty(text);
        }

        private static Material EnsureMaterial(
            TMP_FontAsset font,
            string path,
            string materialName,
            Color underlayColor)
        {
            var source = font.material;
            if (source == null)
                throw new InvalidOperationException($"TMP font '{font.name}' has no source material.");

            EnsureFolder("Assets/Game/Content/UI/Fonts/Materials");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.CopyPropertiesFromMaterial(source);
                material.shader = source.shader;
                material.name = materialName;
            }

            material.DisableKeyword("BEVEL_ON");
            material.DisableKeyword("GLOW_ON");
            material.DisableKeyword("OUTLINE_ON");
            material.DisableKeyword("UNDERLAY_INNER");
            material.EnableKeyword("UNDERLAY_ON");

            SetTexture(material, "_MainTex", source.GetTexture("_MainTex"));
            SetTexture(material, "_FaceTex", null);
            SetColor(material, "_FaceColor", Color.white);
            SetColor(material, "_OutlineColor", Color.black);
            SetFloat(material, "_OutlineWidth", 0f);
            SetFloat(material, "_OutlineSoftness", 0f);
            SetColor(material, "_UnderlayColor", underlayColor);
            SetFloat(material, "_UnderlayDilate", 1f);
            SetFloat(material, "_UnderlayOffsetX", 0f);
            SetFloat(material, "_UnderlayOffsetY", 0f);
            SetFloat(material, "_UnderlaySoftness", 0f);
            SetFloat(material, "_ScaleRatioB", 1f);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
                material.SetFloat(property, value);
        }

        private static void SetColor(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
                material.SetColor(property, value);
        }

        private static void SetTexture(Material material, string property, Texture value)
        {
            if (material.HasProperty(property))
                material.SetTexture(property, value);
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }
    }

    public enum RecoveredTextStyle
    {
        DarkWithLightBorder,
        LightWithDarkBorder,
        StageNumber,
    }
}
