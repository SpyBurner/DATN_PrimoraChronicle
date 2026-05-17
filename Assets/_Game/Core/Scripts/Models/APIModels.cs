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
