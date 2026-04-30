using System;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckButton : MonoBehaviour
{
    [SerializeField] private TMP_Text _deckNameText;
    [SerializeField] private Button _button;

    public void Initialize(DeckSO deckSO, Action onClick)
    {
        _button = gameObject.GetComponent<Button>();
        _deckNameText.text = string.IsNullOrEmpty(deckSO.DeckName) ? "Untitled Deck" : deckSO.DeckName;
        _button.onClick.RemoveAllListeners();

        if (onClick != null)
        {
            _button.onClick.AddListener(() => onClick.Invoke());
        }
    }
}