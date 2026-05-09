using System;
using System.Collections.Generic;

[Serializable]
public class DeckDetailData
{
    public string id;
    public string name;
    public string championStringID;
    public List<string> cardIds = new List<string>();
}
