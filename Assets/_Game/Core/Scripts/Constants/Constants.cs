using Fusion;
using Fusion.Statistics;
using System;
using UnityEngine;
public static class Constants
{
    /// <summary>
    /// UI Identifiers
    /// </summary>
}

public static class SceneNames
{
    public const string BOOTSTRAP = "Bootstrap";

    public const string ACCOUNT = "Account";

    public const string LOBBY = "Lobby";

    public const string GAMEPLAY = "Gameplay";
}

[Serializable]
public enum UILayer
{
    UNSPECIFIED, SCREEN, HUD, POPUP, OVERLAY, SYSTEM
}

[Serializable]
public enum UIIdentifier
{
    UNSPECIFIED = 0,

    // Bootstrap
    BOOTSTRAP_SCENE = 100,
    LOADING_SCREEN = 101,

    // Account
    ACCOUNT_SCENE = 200,
    ACCOUNT_LOGIN = 201,
    ACCOUNT_REGISTER = 202,

    // Lobby
    LOBBY_SCENE = 300,

    LOBBY_MAIN = 301,
    LOBBY_PLAY = 302,
    LOBBY_PROFILE = 303,
    LOBBY_DECK_BUILD = 304,
    DECK_EDITOR = 3041,
    LOBBY_MATCH_HISTORY = 305,
    LOBBY_MATCH_MAKING = 306,
    LOBBY_SHOP = 307,
    LOBBY_CARD_GACHA = 308,
    LOBBY_CHAMPION_UNLOCK = 309,
    LOBBY_SETTING = 310,

    // Popups
    POPUP_CONFIRMATION = 311,
    POPUP_TEXT_INPUT = 312,
    POPUP_DECK_ITEM_CONTEXT = 313,
    POPUP_SHOP_ITEM_CONTEXT = 314,

    // Gameplay
    GAMEPLAY_SCENE = 400,

    GAMEPLAY_LOADING = 401,
    GAMEPLAY_SELECTION = 402,
    GAMEPLAY_MAIN = 403,
    GAMEPLAY_PAUSE = 404,
}
