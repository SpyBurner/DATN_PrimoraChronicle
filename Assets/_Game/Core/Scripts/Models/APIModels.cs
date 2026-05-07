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
public class RegisterRequest
{
    public string username;
    public string password;
}

[System.Serializable]
public class EmptyRequest { }

[System.Serializable]
public class SaveDeckRequest
{
    public string id;
    public string name;
    public List<string> cardIds;
}
