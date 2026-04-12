using UnityEngine;
using Zenject;

internal class LobbyMainController : ILobbyMainController
{
    [Inject] private readonly ILobbyMainModel _model;
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;
    [Inject] private readonly IUIManagerSubsystem _uiManager;

    public void Initialize() { }

    public void NavigateToProfile()
    {
        Debug.Log("Navigate to Profile");
    }

    public void NavigateToBattle()
    {
        Debug.Log("Navigate to Battle");
    }

    public void NavigateToDeck()
    {
        Debug.Log("Navigate to Deck");
    }

    public void NavigateToShop()
    {
        Debug.Log("Navigate to Shop");
    }

    public void NavigateToSettings()
    {
        Debug.Log("Navigate to Settings");
    }

    public void Logout()
    {
        _sceneLoader.LoadScene("Login");
    }
}
