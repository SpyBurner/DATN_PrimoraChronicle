using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ShopPanel : UIPanel
{
    [Inject] private readonly IShopSubsystem _shop;

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }
}
