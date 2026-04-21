using System;
using Zenject;

public class ShopSubsystem : IShopSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IShopController _controller;

    public void Initialize() { }
    public void Dispose() { }

    public void Purchase() => _controller.Purchase();
}
