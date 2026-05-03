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

    private DeckContainerChangeNotifier _deckContainerChangeNotifier;
    private bool _isSyncingDeckPlaceholder;

    protected override void OnEnable()
    {
        base.OnEnable();
        ConfigureDeckContainerWatcher();
        _saveButton?.onClick.AddListener(OnSave);
        RenderCardDisplays();
    }

    protected override void OnDisable()
    {
        if (_deckContainerChangeNotifier != null)
        {
            _deckContainerChangeNotifier.Changed -= HandleDeckContainerChanged;
        }

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
        SyncDeckPlaceholder();
        RenderCards(_deckEdit.GetChampionCards(), _championCardContainer);
        RenderCards(_deckEdit.GetAvailableCards(), _cardContainer, HandleAvailableCardClicked);
        _cardDisplayPrefab.gameObject.SetActive(false);
    }

    private void ConfigureDeckContainerWatcher()
    {
        if (_deckContainer == null)
        {
            return;
        }

        _deckContainerChangeNotifier = _deckContainer.GetComponent<DeckContainerChangeNotifier>();
        if (_deckContainerChangeNotifier == null)
        {
            _deckContainerChangeNotifier = _deckContainer.AddComponent<DeckContainerChangeNotifier>();
        }

        _deckContainerChangeNotifier.Changed -= HandleDeckContainerChanged;
        _deckContainerChangeNotifier.Changed += HandleDeckContainerChanged;
    }

    private void HandleDeckContainerChanged()
    {
        SyncDeckPlaceholder();
    }

    private void SyncDeckPlaceholder()
    {
        if (_isSyncingDeckPlaceholder || _deckContainer == null || _cardDisplayPrefab == null)
        {
            return;
        }

        _isSyncingDeckPlaceholder = true;

        try
        {
            bool hasRealCard = false;
            CardDisplay placeholder = null;

            for (int childIndex = 0; childIndex < _deckContainer.transform.childCount; childIndex++)
            {
                Transform child = _deckContainer.transform.GetChild(childIndex);
                if (child == _cardDisplayPrefab.transform)
                {
                    continue;
                }

                CardDisplay cardDisplay = child.GetComponent<CardDisplay>();
                if (cardDisplay == null)
                {
                    continue;
                }

                if (string.Equals(child.name, "EmptyCardDisplay", StringComparison.Ordinal))
                {
                    placeholder = cardDisplay;
                    continue;
                }

                hasRealCard = true;
            }

            if (hasRealCard)
            {
                if (placeholder != null)
                {
                    Destroy(placeholder.gameObject);
                }

                return;
            }

            if (placeholder == null)
            {
                CreateCardDisplay(null, _deckContainer.transform);
            }
        }
        finally
        {
            _isSyncingDeckPlaceholder = false;
        }
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

    private sealed class DeckContainerChangeNotifier : MonoBehaviour
    {
        public event Action Changed;

        private void OnTransformChildrenChanged()
        {
            Changed?.Invoke();
        }
    }
}
