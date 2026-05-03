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
            SetImageVisible(_cardIllustration, false);
            SetImageVisible(_cardFrame, false);
            return;
        }

        SetImageSprite(_cardIllustration, cardSO.CardIllustration);
        SetImageVisible(_cardFrame, true);
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