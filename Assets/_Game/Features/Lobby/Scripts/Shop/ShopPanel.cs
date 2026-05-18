using System.Collections.Generic;
using System.Linq;
using Core.GDS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ShopPanel : UIPanel
{
    [Inject] private readonly IShopSubsystem _shop;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoadingManager;
    [Inject] private readonly ILobbyMainSubsystem _lobbyMain;

    [SerializeField] private GameObject _cardDisplayPrefab;
    [SerializeField] private GameObject _dailyDealContainer;
    [SerializeField] private GameObject _commonCardsContainer;
    [SerializeField] private CardDisplay _championCardDisplay;
    [SerializeField] private TMP_Text _userGoldText;
    [SerializeField] private TMP_Text _errorText;

    protected override void OnEnable()
    {
        base.OnEnable();
        _shop.DailyDealCardsChanged += OnDailyDealCardsChanged;
        _shop.CommonCardsChanged += OnCommonCardsChanged;
        _shop.UserGoldChanged += OnUserGoldChanged;
        _shop.ErrorMessageChanged += OnErrorMessageChanged;
        _lobbyMain.GoldChanged += OnUserGoldChanged;
        SetRandomChampionCardDisplay();
        _shop.GenerateShopCards();
        _lobbyMain.Refresh();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _shop.DailyDealCardsChanged -= OnDailyDealCardsChanged;
        _shop.CommonCardsChanged -= OnCommonCardsChanged;
        _shop.UserGoldChanged -= OnUserGoldChanged;
        _shop.ErrorMessageChanged -= OnErrorMessageChanged;
        _lobbyMain.GoldChanged -= OnUserGoldChanged;
    }

    private void OnUserGoldChanged(int gold)
    {
        if (_userGoldText != null)
            _userGoldText.text = gold.ToString();
    }

    private void OnErrorMessageChanged(string message)
    {
        if (_errorText != null)
        {
            _errorText.text = message;
            _errorText.enabled = !string.IsNullOrEmpty(message);
        }
    }

    private void OnDailyDealCardsChanged(List<ShopCardSlot> slots)
    {
        PopulateContainer(_dailyDealContainer, slots);
    }

    private void OnCommonCardsChanged(List<ShopCardSlot> slots)
    {
        PopulateContainer(_commonCardsContainer, slots);
    }

    private void PopulateContainer(GameObject container, List<ShopCardSlot> slots)
    {
        if (container == null || _cardDisplayPrefab == null) return;

        foreach (Transform child in container.transform)
            Destroy(child.gameObject);

        foreach (var slot in slots)
        {
            var go = Instantiate(_cardDisplayPrefab, container.transform);
            var cardDisplay = go.GetComponentInChildren<CardDisplay>();
            if (cardDisplay == null) continue;

            _cardLoadingManager.TryGetCard(slot.StringID, out var cardSO);
            _cardLoadingManager.TryGetCardData(slot.StringID, out var cardData);

            var skillNames = new List<string>();
            if (cardData != null)
            {
                if (!string.IsNullOrEmpty(cardData.grants_skill) &&
                    _cardLoadingManager.TryGetSkillData(cardData.grants_skill, out var singleSkill))
                    skillNames.Add(singleSkill.name);
                if (cardData.grants_skills != null)
                    foreach (var skillId in cardData.grants_skills)
                        if (_cardLoadingManager.TryGetSkillData(skillId, out var skill))
                            skillNames.Add(skill.name);
            }

            cardDisplay.SetCardInfo(cardSO, cardData, skillNames);

            var button = go.GetComponentInChildren<Button>();
            if (button != null)
            {
                var stringId = slot.StringID;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _shop.PurchaseItem(stringId));
            }
        }
    }

    private void SetRandomChampionCardDisplay()
    {
        if (_championCardDisplay == null) return;

        var champions = _cardLoadingManager.GetChampionCardsList();
        if (champions == null || champions.Count == 0) return;

        var randomChampion = champions.Values.ElementAt(UnityEngine.Random.Range(0, champions.Count));
        _cardLoadingManager.TryGetCardData(randomChampion.StringID, out var cardData);

        var skillNames = new List<string>();
        if (!string.IsNullOrEmpty(cardData?.grants_skill) &&
            _cardLoadingManager.TryGetSkillData(cardData.grants_skill, out var singleSkill))
            skillNames.Add(singleSkill.name);
        if (cardData?.grants_skills != null)
            foreach (var skillId in cardData.grants_skills)
                if (_cardLoadingManager.TryGetSkillData(skillId, out var skill))
                    skillNames.Add(skill.name);

        _championCardDisplay.SetCardInfo(randomChampion, cardData, skillNames);

        var button = _championCardDisplay.GetComponentInChildren<Button>();
        if (button != null)
        {
            var stringId = randomChampion.StringID;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _shop.PurchaseItem(stringId));
        }
    }
}
