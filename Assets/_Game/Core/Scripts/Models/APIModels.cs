using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UserDataResponse
{
    public string ID;
    public string username;
    public int gold;
}

[System.Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

[System.Serializable]
public class LoginResponse
{
    public string token;
    public UserDataResponse user;
}

[System.Serializable]
public class RegisterRequest
{
    public string username;
    public string password;
}

[System.Serializable]
public class RegisterResponse
{
    public UserDataResponse user;
    public string token; // Optional, some APIs return it upon registration
}

[System.Serializable]
public class EmptyRequest { }

[System.Serializable]
public class SaveDeckRequest
{
    public string id;
    public string name;
    public string championStringID;
    public List<string> cardIds;
}

[System.Serializable]
public class StartSessionCommand
{
    public string SessionName;
    public string Player1UserId;
    public string Player2UserId;
    public int RegionCode;
}

[System.Serializable]
public class MatchResultData
{
    public string SessionName;
    public string WinnerUserId;
    public string LoserUserId;
    public int DurationSeconds;
    public string EndReason; // "Normal", "Disconnect", "Timeout"
}

[System.Serializable]
public class BackendBridgeStateData
{
    public StartSessionCommand PendingStartSession;
    public bool IsListening;
}

[System.Serializable]
public class ServerSessionStateData
{
    public string ActiveSessionName;
    public bool IsRunning;
    public Fusion.PlayerRef LastJoinedPlayer;
    public Fusion.PlayerRef LastLeftPlayer;
}

[System.Serializable]
public class PlayerDisconnectedPayload
{
    public string userId;
}


