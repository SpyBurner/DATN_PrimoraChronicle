using System.Collections.Generic;
using UnityEngine.Events;

public interface IHandSubsystem : ISubsystem
{
    event UnityAction<List<string>> CardsChanged;
    void PlayCard(string cardId);
}
