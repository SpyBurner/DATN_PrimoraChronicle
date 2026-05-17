using System;
using Core;
using Core.GDS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    [SerializeField] private Image _cardIllustration;
    [SerializeField] private TMP_Text _cardNameText;
    //[SerializeField] private Image _cardFrame;

    public void SetCardInfo(CardSO cardSO, CardData cardData = null)
    {
        if (cardSO == null || cardData == null)
        {
            SetImageVisible(_cardIllustration, false);
            // SetImageVisible(_cardFrame, false);
            return;
        }

        SetImageSprite(_cardIllustration, cardSO.CardIllustration);
        SetText(_cardNameText, cardData?.name);
        //SetImageVisible(_cardFrame, true);
    }

    private static void SetImageSprite(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private static void SetText(TMP_Text textComponent, string text)
    {
        if (textComponent == null)
        {
            return;
        }

        textComponent.text = text;
        textComponent.enabled = !string.IsNullOrEmpty(text);
    }

    private static void SetImageVisible(Image image, bool isVisible)
    {
        if (image == null)
        {
            return;
        }

        if (!isVisible)
        {
            image.sprite = null;
        }

        image.enabled = isVisible;
    }
}