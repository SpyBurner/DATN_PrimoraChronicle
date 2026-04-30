using System;
using Core;
using UnityEngine;
using UnityEngine.UI;
public class CardDisplay : MonoBehaviour
{
    [SerializeField] private Image _cardIllustration;
    [SerializeField] private Image _cardFrame;

    public void SetCardInfo(CardSO cardSO)
    {
        if (cardSO == null)
        {
            Debug.LogError("CardSO is null. Cannot set card info.");
            return;
        }
        _cardIllustration.sprite = cardSO.CardIllustration;
    }
}