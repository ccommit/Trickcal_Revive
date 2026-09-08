using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrickcalRevive.MainUI
{
    /// <summary>스테이지 정보 팝업. ★·이름·권장전투력·추천성격·출현몬스터·보상 + 면제/덱선택/닫기.</summary>
    public sealed class StageInfoPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text stageNameLabel;
        [SerializeField] private Image[] stars;
        [SerializeField] private TMP_Text powerLabel;
        [SerializeField] private TMP_Text personalityLabel;
        [SerializeField] private Image personalityIcon;
        [SerializeField] private Image[] monsterSlots;
        [SerializeField] private Image[] rewardIcons;
        [SerializeField] private TMP_Text[] rewardAmounts;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button deckButton;
        [SerializeField] private Button exemptButton;

        // 알려진 4 몬스터 / 4 통화 스프라이트(빌더가 주입).
        [SerializeField] private Sprite monsterGluttonbear;
        [SerializeField] private Sprite monsterLupalu;
        [SerializeField] private Sprite monsterMagicfork;
        [SerializeField] private Sprite monsterPumpkin;
        [SerializeField] private Sprite rewardGold;
        [SerializeField] private Sprite rewardMacaron;
        [SerializeField] private Sprite rewardElleaf;
        [SerializeField] private Sprite rewardStamina;
        [SerializeField] private Sprite personalityNaive;
        [SerializeField] private Sprite personalityMad;
        [SerializeField] private Sprite personalityJolly;
        [SerializeField] private Sprite personalityGloomy;
        [SerializeField] private Sprite personalityCool;

        private static readonly Color EarnedStar = Color.white;
        private static readonly Color EmptyStar = new Color32(70, 70, 70, 200);

        public event Action CloseRequested;
        public event Action DeckRequested;

        public bool IsReady =>
            panel != null && stageNameLabel != null && powerLabel != null && personalityLabel != null
            && personalityIcon != null
            && stars != null && stars.Length == 3
            && monsterSlots != null && monsterSlots.Length > 0
            && rewardIcons != null && rewardAmounts != null && rewardIcons.Length == rewardAmounts.Length
            && closeButton != null && deckButton != null;

        public void Configure(
            GameObject panelRoot,
            TMP_Text stageName,
            Image[] starImages,
            TMP_Text power,
            TMP_Text personality,
            Image personalityImage,
            Image[] monsters,
            Image[] rewardIconImages,
            TMP_Text[] rewardAmountLabels,
            Button close,
            Button deck,
            Button exempt,
            Sprite gluttonbear,
            Sprite lupalu,
            Sprite magicfork,
            Sprite pumpkin,
            Sprite gold,
            Sprite macaron,
            Sprite elleaf,
            Sprite stamina,
            Sprite naive,
            Sprite mad,
            Sprite jolly,
            Sprite gloomy,
            Sprite cool)
        {
            panel = panelRoot;
            stageNameLabel = stageName;
            stars = starImages;
            powerLabel = power;
            personalityLabel = personality;
            personalityIcon = personalityImage;
            monsterSlots = monsters;
            rewardIcons = rewardIconImages;
            rewardAmounts = rewardAmountLabels;
            closeButton = close;
            deckButton = deck;
            exemptButton = exempt;
            monsterGluttonbear = gluttonbear;
            monsterLupalu = lupalu;
            monsterMagicfork = magicfork;
            monsterPumpkin = pumpkin;
            rewardGold = gold;
            rewardMacaron = macaron;
            rewardElleaf = elleaf;
            rewardStamina = stamina;
            personalityNaive = naive;
            personalityMad = mad;
            personalityJolly = jolly;
            personalityGloomy = gloomy;
            personalityCool = cool;
        }

        private void Awake() => Hide();

        private void OnEnable()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(RaiseClose);
            if (deckButton != null)
                deckButton.onClick.AddListener(RaiseDeck);
        }

        private void OnDisable()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(RaiseClose);
            if (deckButton != null)
                deckButton.onClick.RemoveListener(RaiseDeck);
        }

        public void Show(StageInfoData data)
        {
            if (stageNameLabel != null)
                stageNameLabel.text = data.StageName;
            if (powerLabel != null)
                powerLabel.text = $"권장 전투력 {data.RecommendedPower:N0}";
            if (personalityLabel != null)
                personalityLabel.text = data.PersonalityText;
            if (personalityIcon != null)
                personalityIcon.sprite = SpriteForPersonality(data.PersonalityText);
            if (stars != null)
            {
                for (var i = 0; i < stars.Length; i++)
                    if (stars[i] != null)
                        stars[i].color = i < data.Stars ? EarnedStar : EmptyStar;
            }
            if (monsterSlots != null)
            {
                for (var i = 0; i < monsterSlots.Length; i++)
                {
                    if (monsterSlots[i] == null)
                        continue;
                    var has = data.MonsterIds != null && i < data.MonsterIds.Count;
                    monsterSlots[i].gameObject.SetActive(has);
                    if (has)
                        monsterSlots[i].sprite = SpriteForMonster(data.MonsterIds[i]);
                }
            }
            if (rewardIcons != null && rewardAmounts != null)
            {
                for (var i = 0; i < rewardIcons.Length; i++)
                {
                    var has = data.Rewards != null && i < data.Rewards.Count;
                    if (rewardIcons[i] != null)
                    {
                        rewardIcons[i].gameObject.SetActive(has);
                        if (has)
                            rewardIcons[i].sprite = SpriteForReward(data.Rewards[i].RewardType);
                    }
                    if (rewardAmounts[i] != null)
                    {
                        rewardAmounts[i].gameObject.SetActive(has);
                        if (has)
                            rewardAmounts[i].text = data.Rewards[i].Amount.ToString("N0");
                    }
                }
            }
            if (panel != null)
                panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        private Sprite SpriteForMonster(string id)
        {
            switch (id)
            {
                case "gluttonbear": return monsterGluttonbear;
                case "lupalu": return monsterLupalu;
                case "magicfork": return monsterMagicfork;
                case "pumpkin": return monsterPumpkin;
                default: return monsterGluttonbear;
            }
        }

        private Sprite SpriteForReward(string type)
        {
            switch (type)
            {
                case "Gold": return rewardGold;
                case "Macaron": return rewardMacaron;
                case "Elleaf": return rewardElleaf;
                case "Stamina": return rewardStamina;
                default: return rewardGold;
            }
        }

        private Sprite SpriteForPersonality(string personality)
        {
            switch (personality)
            {
                case "순수":
                case "Naive": return personalityNaive;
                case "광기":
                case "Mad": return personalityMad;
                case "활발":
                case "Jolly": return personalityJolly;
                case "우울":
                case "Gloomy": return personalityGloomy;
                case "냉정":
                case "Cool": return personalityCool;
                default: return personalityNaive;
            }
        }

        private void RaiseClose() => CloseRequested?.Invoke();
        private void RaiseDeck() => DeckRequested?.Invoke();
    }
}
