using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using TrickcalRevive.App.Editor;

namespace TrickcalRevive.MainUI.Tests
{
    public sealed class RecoveredTextStyleTests
    {
        [Test]
        public void RecoveredMaterials_MatchTheSelectedOriginalPresets()
        {
            var whiteBorder = LoadMaterial(RecoveredTextStyles.WhiteBorderMaterialPath);
            var blackBorder = LoadMaterial(RecoveredTextStyles.BlackBorderMaterialPath);
            var battleList = LoadMaterial(RecoveredTextStyles.BattleListMaterialPath);

            AssertUnderlay(whiteBorder, Color.white);
            AssertUnderlay(blackBorder, Color.black);
            AssertUnderlay(battleList, new Color32(38, 32, 29, 255));
        }

        [Test]
        public void LobbyAndSharedPageTitle_UseDarkFaceWithWhiteBorder()
        {
            var lobby = LoadPrefab(LoginLobbyUIBuilder.LobbyPrefabPath);
            var topPanel = LoadPrefab(TopCurrencyPanelBuilder.PrefabPath);

            var navigationButtonNames = new[] { "RecruitButton", "ApostleButton", "AdventureButton" };
            var navigationLabels = lobby.GetComponentsInChildren<TMP_Text>(true)
                .Where(text => text.name == "Label" && navigationButtonNames.Contains(text.transform.parent.name))
                .ToArray();
            Assert.That(navigationLabels, Has.Length.EqualTo(3));
            Assert.That(
                navigationLabels.All(text => AssetDatabase.GetAssetPath(text.fontSharedMaterial)
                    == RecoveredTextStyles.WhiteBorderMaterialPath),
                Is.True);
            Assert.That(
                navigationLabels.All(text => ((Color32)text.color).Equals(new Color32(38, 32, 29, 255))),
                Is.True);

            var pageTitle = topPanel.transform.Find("PageTitle").GetComponent<TMP_Text>();
            Assert.That(
                AssetDatabase.GetAssetPath(pageTitle.fontSharedMaterial),
                Is.EqualTo(RecoveredTextStyles.WhiteBorderMaterialPath));
            Assert.That((Color32)pageTitle.color, Is.EqualTo(new Color32(38, 32, 29, 255)));
        }

        [Test]
        public void StageSelectAndStageInfo_UseTheirScreenSpecificTextTreatment()
        {
            var stageSelect = LoadPrefab(StageSelectUIBuilder.StageSelectPrefabPath);
            var stageNumbers = stageSelect.GetComponentsInChildren<TMP_Text>(true)
                .Where(text => text.name == "Number" && text.transform.parent.name == "NumberPlate")
                .ToArray();
            Assert.That(stageNumbers, Is.Not.Empty);
            Assert.That(
                stageNumbers.All(text => AssetDatabase.GetAssetPath(text.fontSharedMaterial)
                    == RecoveredTextStyles.BattleListMaterialPath),
                Is.True);

            foreach (var tabName in new[] { "Tab_Mild", "Tab_Hot", "Tab_Fire" })
            {
                var label = stageSelect.transform.Find(tabName + "/Label").GetComponent<TMP_Text>();
                Assert.That((Color32)label.color, Is.EqualTo(new Color32(47, 45, 47, 255)));
            }

            var popup = LoadPrefab(StageInfoPopupUIBuilder.PopupPrefabPath);
            var closeIcon = popup.transform.Find("Content/Panel/CloseButton").GetComponent<Image>();
            var exemptLabel = popup.transform.Find("Content/Panel/ButtonGroup/ExemptButton/Label").GetComponent<TMP_Text>();
            var deckLabel = popup.transform.Find("Content/Panel/ButtonGroup/DeckButton/Label").GetComponent<TMP_Text>();
            Assert.That(AssetDatabase.GetAssetPath(closeIcon.sprite), Does.EndWith("CommonButton_Close_1.png"));
            Assert.That((Color32)exemptLabel.color, Is.EqualTo(new Color32(55, 51, 48, 255)));
            Assert.That((Color32)deckLabel.color, Is.EqualTo(new Color32(55, 51, 48, 255)));
        }

        [Test]
        public void PartySetup_ReadabilityLabelsUseWhiteFaceWithBlackBorder()
        {
            var party = LoadPrefab(PartySetupUIBuilder.PartyPrefabPath);
            var status = party.transform.Find("Status").GetComponent<TMP_Text>();
            Assert.That(
                AssetDatabase.GetAssetPath(status.fontSharedMaterial),
                Is.EqualTo(RecoveredTextStyles.BlackBorderMaterialPath));

            var columnLabels = party.GetComponentsInChildren<TMP_Text>(true)
                .Where(text => text.name == "Label" && text.transform.parent.name.StartsWith("Column_"))
                .ToArray();
            Assert.That(columnLabels, Has.Length.EqualTo(3));
            Assert.That(
                columnLabels.All(text => AssetDatabase.GetAssetPath(text.fontSharedMaterial)
                    == RecoveredTextStyles.BlackBorderMaterialPath),
                Is.True);
        }

        private static Material LoadMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.That(material, Is.Not.Null, path);
            return material;
        }

        private static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            return prefab;
        }

        private static void AssertUnderlay(Material material, Color expectedColor)
        {
            Assert.That(material.IsKeywordEnabled("UNDERLAY_ON"), Is.True, material.name);
            Assert.That(material.IsKeywordEnabled("OUTLINE_ON"), Is.False, material.name);
            Assert.That(material.GetFloat("_OutlineWidth"), Is.Zero.Within(0.0001f), material.name);
            Assert.That(material.GetFloat("_UnderlayDilate"), Is.EqualTo(1f).Within(0.0001f), material.name);
            Assert.That(material.GetFloat("_UnderlayOffsetX"), Is.Zero.Within(0.0001f), material.name);
            Assert.That(material.GetFloat("_UnderlayOffsetY"), Is.Zero.Within(0.0001f), material.name);
            Assert.That(material.GetFloat("_UnderlaySoftness"), Is.Zero.Within(0.0001f), material.name);
            Assert.That(material.GetColor("_UnderlayColor"), Is.EqualTo(expectedColor).Using(ColorComparer));
        }

        private static readonly IEqualityComparer<Color> ColorComparer =
            new ApproximateColorComparer(0.0001f);

        private sealed class ApproximateColorComparer : IEqualityComparer<Color>
        {
            private readonly float tolerance;

            public ApproximateColorComparer(float tolerance)
            {
                this.tolerance = tolerance;
            }

            public bool Equals(Color left, Color right) =>
                Mathf.Abs(left.r - right.r) <= tolerance
                && Mathf.Abs(left.g - right.g) <= tolerance
                && Mathf.Abs(left.b - right.b) <= tolerance
                && Mathf.Abs(left.a - right.a) <= tolerance;

            public int GetHashCode(Color value) => value.GetHashCode();
        }
    }
}
