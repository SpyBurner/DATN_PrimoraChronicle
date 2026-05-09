using Fusion;
using Fusion.Statistics;
using System;
using UnityEngine;
public static class Constants
{
    /// <summary>
    /// UI Identifiers
    /// </summary>
    public const int DECK_CARD_COUNT = 20;
}

public static class HttpErrors
{
    public const string DEFAULT = "An unexpected error occurred. Please try again.";
    public const string TIMEOUT = "Request timed out. Please check your internet connection.";
    public const string NETWORK_ERROR = "Cannot connect to server. Please check your internet connection.";
    public const string UNAUTHORIZED = "Session expired. Please login again.";
    public const string FORBIDDEN = "Access denied.";
    public const string NOT_FOUND = "Requested resource not found.";
    public const string SERVER_ERROR = "Server encountered an internal error. Please try again later.";

    // Backend Specific Errors
    public const string INVALID_CREDENTIALS = "Invalid username or password.";
    public const string USERNAME_TAKEN = "Username is already registered.";
    public const string DECK_SIZE_INVALID = "Deck must contain exactly 20 cards.";
    public const string CARD_NOT_OWNED = "You do not own enough copies of one or more cards in this deck.";
    public const string CARD_NOT_FOUND = "One or more cards in your deck do not exist.";
    public const string CARD_INVALID_RARITY = "Only 'Common' rarity cards are allowed in this deck.";
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
    GAMEPLAY_FUSION = 405,
    GAMEPLAY_SKILL = 406,
    GAMEPLAY_DRAW = 407,
    GAMEPLAY_MATCH_RESULT = 408,
    GAMEPLAY_TURN_ORDER = 409,
    GAMEPLAY_HAND = 410,
}
