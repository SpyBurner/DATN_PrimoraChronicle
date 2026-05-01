using System.Collections.Generic;
using System;
using Core;
using Zenject;
using UnityEngine;
using UnityEngine.UI;

public class DeckEditPanel : UIPanel
{
    [Inject] private readonly IDeckEditSubsystem _deckEdit;

    [SerializeField] private CardDisplay _cardDisplayPrefab;
    [SerializeField] private GameObject _championCardContainer;
    [SerializeField] private GameObject _deckContainer;
    [SerializeField] private GameObject _cardContainer;
    [SerializeField] private Image _championPortrait;
    [SerializeField] private Button _saveButton;

    protected override void OnEnable()
    {
        base.OnEnable();
        _saveButton?.onClick.AddListener(OnSave);
        RenderCardDisplays();
    }

    protected override void OnDisable()
    {
        ClearContainer(_deckContainer, _cardDisplayPrefab);
        ClearContainer(_cardContainer, _cardDisplayPrefab);
        ClearContainer(_championCardContainer, _cardDisplayPrefab);
        _saveButton?.onClick.RemoveListener(OnSave);
        base.OnDisable();
    }

    private void RenderCardDisplays()
    {
        ClearContainer(_deckContainer, _cardDisplayPrefab);
        ClearContainer(_cardContainer, _cardDisplayPrefab);
        ClearContainer(_championCardContainer, _cardDisplayPrefab);


        ChampionCardSO championCard = _deckEdit.GetChampionCard();
        if (_championPortrait != null)
        {
            _championPortrait.sprite = championCard != null ? championCard.CardIllustration : null;
        }

        if (_cardDisplayPrefab == null)
        {
            return;
        }

        RenderCards(_deckEdit.GetDeckCards(), _deckContainer, HandleDeckCardClicked);
        RenderCards(_deckEdit.GetChampionCards(), _championCardContainer);
        RenderCards(_deckEdit.GetAvailableCards(), _cardContainer, HandleAvailableCardClicked);
        _cardDisplayPrefab.gameObject.SetActive(false);
    }

    private void RenderCards(IEnumerable<CardSO> cards, GameObject container, Action<CardSO> onClick = null)
    {
        if (container == null || cards == null)
        {
            return;
        }

        foreach (CardSO card in cards)
        {
            if (card == null)
            {
                continue;
            }

            CreateCardDisplay(card, container.transform, onClick);
        }
    }

    private void CreateCardDisplay(CardSO card, Transform parent, Action<CardSO> onClick = null)
    {
        CardDisplay cardDisplay = Instantiate(_cardDisplayPrefab, parent);
        cardDisplay.gameObject.name = card.name;
        cardDisplay.gameObject.SetActive(true);
        cardDisplay.SetCardInfo(card);

        Button button = cardDisplay.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();

            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick.Invoke(card));
            }
        }
    }

    private void HandleAvailableCardClicked(CardSO card)
    {
        if (_deckEdit.TryAddCardToSelectedDeck(card))
        {
            RenderCardDisplays();
        }
    }

    private void HandleDeckCardClicked(CardSO card)
    {
        if (_deckEdit.TryRemoveCardFromSelectedDeck(card))
        {
            RenderCardDisplays();
        }
    }

    private void OnSave()
    {
        _deckEdit.SaveSelectedDeck();
    }

    private static void ClearContainer(GameObject container, CardDisplay preservedDisplay)
    {
        if (container == null)
        {
            return;
        }

        for (int childIndex = container.transform.childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform child = container.transform.GetChild(childIndex);
            if (preservedDisplay != null && child == preservedDisplay.transform)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }
}
