using UnityEngine.Events;
using UnityEngine;

/// <summary>
/// This class is used for Events that have a callback argument.
/// </summary>

[CreateAssetMenu(menuName = "Events/Callback Event Channel")]
public class CallbackEventChannelSO : DescriptionBaseSO
{
	public event UnityAction<System.Action> OnEventRaised;

	public void RaiseEvent(System.Action callback)
	{
        OnEventRaised?.Invoke(callback);
    }
}
