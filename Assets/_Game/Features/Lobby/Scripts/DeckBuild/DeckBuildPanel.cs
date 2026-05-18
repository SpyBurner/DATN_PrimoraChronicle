using System.Collections.Generic;
using System;
using System.Text;
using System.Threading.Tasks;
using Core;
using Zenject;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckBuildPanel : UIPanel
{
    [Inject] private readonly IDeckBuildSubsystem _deckBuild;
    [Inject] private readonly IDeckBuildModel _deckBuildModel;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoadingManager;

    [SerializeField] private CardDisplay _cardDisplayPrefab;
    [SerializeField] private GameObject _championGrantedCardsContainer;
    [SerializeField] private GameObject _deckContainer;
    [SerializeField] private GameObject _cardContainer;
    [SerializeField] private Image _championPortrait;
    [SerializeField] private TMP_Text _championDescription;
    [SerializeField] private Button _saveButton;
    [SerializeField] private TMP_Text _errorText;
    [SerializeField] private Button _championChoose;
    [SerializeField] private GameObject _championChooseContainer;
    [SerializeField] private GameObject _championChooseOverlay;

    protected override void OnEnable()
    {
        base.OnEnable();

        _deckBuild.DeckCardsChanged += HandleDeckCardsChanged;
        _deckBuild.ChampionCardsChanged += HandleChampionCardsChanged;
        _deckBuild.ChampionGrantedCardsChanged += HandleChampionGrantedCardsChanged;
        _deckBuild.AvailableCardsChanged += HandleAvailableCardsChanged;
        _deckBuild.ErrorMessageChanged += OnErrorMessageChanged;

        _saveButton?.onClick.AddListener(OnSave);
        _championChoose?.onClick.AddListener(OnChampionChooseClicked);

        RefreshAll();
        _ = _deckBuild.LoadAvailableCards();
    }

    protected override void OnDisable()
    {
        _deckBuild.DeckCardsChanged -= HandleDeckCardsChanged;
        _deckBuild.ChampionCardsChanged -= HandleChampionCardsChanged;
        _deckBuild.ChampionGrantedCardsChanged -= HandleChampionGrantedCardsChanged;
        _deckBuild.AvailableCardsChanged -= HandleAvailableCardsChanged;
        _deckBuild.ErrorMessageChanged -= OnErrorMessageChanged;

        _saveButton?.onClick.RemoveListener(OnSave);
        _championChoose?.onClick.RemoveListener(OnChampionChooseClicked);

        ClearAll();
        base.OnDisable();
    }

    private void RefreshAll()
    {
        if (_deckBuildModel == null)
        {
            return;
        }

        HandleDeckCardsChanged(_deckBuildModel.DeckCards.Value);
        HandleChampionCardsChanged(_deckBuildModel.ChampionCards.Value);
        HandleChampionGrantedCardsChanged(_deckBuildModel.ChampionGrantedCards.Value);
        HandleAvailableCardsChanged(_deckBuildModel.AvailableCards.Value);
    }

    private void ClearAll()
    {
        ClearContainer(_deckContainer);
        ClearContainer(_cardContainer);
        ClearContainer(_championGrantedCardsContainer);
    }

    private void HandleDeckCardsChanged(IReadOnlyList<CardSO> cards)
    {
        ClearContainer(_deckContainer);
        if (cards == null || cards.Count == 0)
        {
            CreateCardDisplay(null, _deckContainer.transform); // Placeholder
        }
        else
        {
            RenderCards(cards, _deckContainer, HandleDeckCardClicked);
        }
    }

    private void HandleChampionCardsChanged(IReadOnlyList<CardSO> cards)
    {
        CardSO champion = (cards != null && cards.Count > 0) ? cards[0] : null;

        if (_championPortrait != null)
        {
            _championPortrait.sprite = champion != null ? champion.CardIllustration : null;
        }

        if (_championDescription != null)
        {
            _championDescription.text = BuildChampionDescription(champion);
        }
    }

    private string BuildChampionDescription(CardSO champion)
    {
        if (champion == null || !_cardLoadingManager.TryGetCardData(champion.StringID, out var cardData))
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine(cardData.name);
        sb.AppendLine($"Faction: {cardData.faction}          HP: {cardData.hp}           Death Anchor: {cardData.death_anchor}         Speed: {cardData.speed}");

        if (!string.IsNullOrEmpty(cardData.grants_skill) &&
            _cardLoadingManager.TryGetSkillData(cardData.grants_skill, out var skillData))
        {
            sb.AppendLine();
            sb.AppendLine(skillData.name);
            sb.Append(skillData.description);
        }

        return sb.ToString();
    }

    private void HandleChampionGrantedCardsChanged(IReadOnlyList<CardSO> cards)
    {
        ClearContainer(_championGrantedCardsContainer);
        RenderCards(cards, _championGrantedCardsContainer);
    }

    private void HandleAvailableCardsChanged(IReadOnlyList<CardSO> cards)
    {
        ClearContainer(_cardContainer);

        if (_cardContainer != null)
        {
            _cardContainer.SetActive(cards != null && cards.Count > 0);
        }

        RenderCards(cards, _cardContainer, HandleAvailableCardClicked);
    }

    private void RenderCards(IEnumerable<CardSO> cards, GameObject container, Action<CardSO> onClick = null)
    {
        if (container == null || cards == null) return;

        foreach (CardSO card in cards)
        {
            if (card == null) continue;
            CreateCardDisplay(card, container.transform, onClick);
        }
    }

    private void CreateCardDisplay(CardSO card, Transform parent, Action<CardSO> onClick = null)
    {
        if (_cardDisplayPrefab == null) return;

        CardDisplay cardDisplay = Instantiate(_cardDisplayPrefab, parent);
        cardDisplay.gameObject.name = card != null ? card.name : "EmptyCardDisplay";
        cardDisplay.gameObject.SetActive(true);

        Core.GDS.CardData cardData = null;
        if (card != null) _cardLoadingManager.TryGetCardData(card.StringID, out cardData);

        var skillNames = new List<string>();
        if (!string.IsNullOrEmpty(cardData?.grants_skill) &&
            _cardLoadingManager.TryGetSkillData(cardData.grants_skill, out var singleSkill))
            skillNames.Add(singleSkill.name);
        if (cardData?.grants_skills != null)
            foreach (var skillId in cardData.grants_skills)
                if (_cardLoadingManager.TryGetSkillData(skillId, out var skill))
                    skillNames.Add(skill.name);

        cardDisplay.SetCardInfo(card, cardData, skillNames);

        Button button = cardDisplay.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = card != null && onClick != null;

            if (card != null && onClick != null)
            {
                button.onClick.AddListener(() => onClick.Invoke(card));
            }
        }
    }

    private void OnChampionChooseClicked()
    {
        if (_championChooseOverlay != null)
            _championChooseOverlay.SetActive(true);

        ClearContainer(_championChooseContainer);

        var champions = _cardLoadingManager.GetChampionCardsList();
        if (champions == null) return;

        foreach (var champion in champions.Values)
            CreateCardDisplay(champion, _championChooseContainer.transform, OnChampionSelected);
    }

    private void OnChampionSelected(CardSO card)
    {
        _deckBuild.AddCardToDeck(card);
        ClearContainer(_championChooseContainer);

        if (_championChooseOverlay != null)
            _championChooseOverlay.SetActive(false);
    }

    private void HandleAvailableCardClicked(CardSO card)
    {
        _deckBuild.AddCardToDeck(card);
    }

    private void HandleDeckCardClicked(CardSO card)
    {
        _deckBuild.RemoveCardFromDeck(card);
    }

    private async void OnSave()
    {
        await _uiManagerSubsystem.Show<DeckSaveConfirmPopup>();

        DeckSaveConfirmPopup popup = _uiManagerSubsystem.GetPanel<DeckSaveConfirmPopup>();
        popup.Setup(_deckBuildModel.CurrentDeckName.Value, deckName => _ = SaveDeckWithName(deckName));
    }

    private async Task SaveDeckWithName(string deckName)
    {
        string trimmedDeckName = deckName?.Trim() ?? string.Empty;
        _deckBuildModel.SetCurrentDeck(_deckBuildModel.CurrentDeckId.Value, trimmedDeckName);
        await _deckBuild.SaveDeck();
    }

    private void OnErrorMessageChanged(string errorMessage)
    {
        if (_errorText != null)
        {
            _errorText.text = errorMessage;
            _errorText.enabled = !string.IsNullOrEmpty(errorMessage);
        }
    }

    private void ClearContainer(GameObject container)
    {
        if (container == null) return;

        for (int childIndex = container.transform.childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform child = container.transform.GetChild(childIndex);
            if (_cardDisplayPrefab != null && child == _cardDisplayPrefab.transform) continue;
            Destroy(child.gameObject);
        }
    }
}
