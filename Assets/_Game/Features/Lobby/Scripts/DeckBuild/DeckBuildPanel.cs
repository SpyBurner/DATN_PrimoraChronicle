using System.Collections.Generic;
using System;
using Core;
using Zenject;
using UnityEngine;
using UnityEngine.UI;

public class DeckBuildPanel : UIPanel
{
    [Inject] private readonly IDeckBuildSubsystem _deckBuild;
    [Inject] private readonly IDeckBuildModel _deckBuildModel;

    [SerializeField] private CardDisplay _cardDisplayPrefab;
    [SerializeField] private GameObject _championCardContainer;
    [SerializeField] private GameObject _deckContainer;
    [SerializeField] private GameObject _cardContainer;
    [SerializeField] private Image _championPortrait;
    [SerializeField] private Button _saveButton;

    protected override void OnEnable()
    {
        base.OnEnable();

        _deckBuild.DeckCardsChanged += HandleDeckCardsChanged;
        _deckBuild.ChampionCardsChanged += HandleChampionCardsChanged;
        _deckBuild.AvailableCardsChanged += HandleAvailableCardsChanged;

        _saveButton?.onClick.AddListener(OnSave);

        RefreshAll();
    }

    protected override void OnDisable()
    {
        _deckBuild.DeckCardsChanged -= HandleDeckCardsChanged;
        _deckBuild.ChampionCardsChanged -= HandleChampionCardsChanged;
        _deckBuild.AvailableCardsChanged -= HandleAvailableCardsChanged;

        _saveButton?.onClick.RemoveListener(OnSave);

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
        HandleAvailableCardsChanged(_deckBuildModel.AvailableCards.Value);
    }

    private void ClearAll()
    {
        ClearContainer(_deckContainer);
        ClearContainer(_cardContainer);
        ClearContainer(_championCardContainer);
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
        ClearContainer(_championCardContainer);
        RenderCards(cards, _championCardContainer);

        if (_championPortrait != null)
        {
            _championPortrait.sprite = (cards != null && cards.Count > 0) ? cards[0].CardIllustration : null;
        }
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
        cardDisplay.SetCardInfo(card);

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

    private void HandleAvailableCardClicked(CardSO card)
    {
        _deckBuild.AddCardToDeck(card);
    }

    private void HandleDeckCardClicked(CardSO card)
    {
        _deckBuild.RemoveCardFromDeck(card);
    }

    private void OnSave()
    {
        _deckBuild.SaveDeck();
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
