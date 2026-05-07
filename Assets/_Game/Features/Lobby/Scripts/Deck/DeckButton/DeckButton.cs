using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckButton : MonoBehaviour
{
    [SerializeField] private TMP_Text _deckNameText;
    [SerializeField] private Button _button;

    public void Initialize(DeckSummaryData deck, Action onClick)
    {
        _button = gameObject.GetComponent<Button>();
        _deckNameText.text = string.IsNullOrEmpty(deck.name) ? "Untitled Deck" : deck.name;
        _button.onClick.RemoveAllListeners();

        if (onClick != null)
        {
            _button.onClick.AddListener(() => onClick.Invoke());
        }
    }
}