using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This class is used for Events that have one float argument.
/// Example: An Achievement unlock event, where the int is the Achievement ID.
/// Example: An player health changed event, need to seed to an UI element
/// </summary>

[CreateAssetMenu(menuName = "Events/String Event Channel")]
public class StringEventChannelSO: DescriptionBaseSO
{
    public UnityAction<string> OnEventRaised;

    public void RaiseEvent(string value)
    {
        OnEventRaised?.Invoke(value);
    }
}
