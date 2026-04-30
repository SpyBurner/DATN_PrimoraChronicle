using System.Collections.Generic;
using Core;
using Zenject;
using UnityEngine;
using System;
using UnityEngine.UI;

public class DeckEditPanel : UIPanel
{
    [Inject] private readonly IDeckEditSubsystem _deckEdit;

    [SerializeField] private CardDisplay _cardDisplayPrefab;
    [SerializeField] private GameObject _championCardContainer;
    [SerializeField] private GameObject _deckContainer;
    [SerializeField] private GameObject _cardContainer;
    [SerializeField] private Image _championPortrait;

    protected override void OnEnable()
    {
        base.OnEnable();
        RenderCardDisplays();
    }

    protected override void OnDisable()
    {
        ClearContainer(_deckContainer);
        ClearContainer(_cardContainer);
        ClearContainer(_championCardContainer);
        base.OnDisable();
    }

    private void RenderCardDisplays()
    {
        ClearContainer(_deckContainer);
        ClearContainer(_cardContainer);
        ClearContainer(_championCardContainer);

        ChampionCardSO championCard = _deckEdit.GetChampionCard();
        if (_championPortrait != null)
        {
            _championPortrait.sprite = championCard != null ? championCard.CardIllustration : null;
        }

        if (_cardDisplayPrefab == null)
        {
            return;
        }

        RenderCards(_deckEdit.GetDeckCards(), _deckContainer);
        RenderCards(_deckEdit.GetChampionCards(), _championCardContainer);
        _cardDisplayPrefab.gameObject.SetActive(false);
    }

    private void RenderCards(IEnumerable<CardSO> cards, GameObject container)
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

            CreateCardDisplay(card, container.transform);
        }
    }

    private void CreateCardDisplay(CardSO card, Transform parent)
    {
        CardDisplay cardDisplay = Instantiate(_cardDisplayPrefab, parent);
        cardDisplay.gameObject.name = card.name;
        cardDisplay.gameObject.SetActive(true);
        cardDisplay.SetCardInfo(card);
    }

    private static void ClearContainer(GameObject container)
    {
        // if (container == null)
        // {
        //     return;
        // }

        // for (int childIndex = container.transform.childCount - 1; childIndex >= 0; childIndex--)
        // {
        //     Destroy(container.transform.GetChild(childIndex).gameObject);
        // }
    }
}