using UnityEngine;
using Zenject;

internal class ShopController : IShopController
{
    [Inject] private readonly IShopModel _model;

    public void Initialize() { }

    public void Purchase()
    {
        Debug.Log("Shop: Purchase");
    }
}
