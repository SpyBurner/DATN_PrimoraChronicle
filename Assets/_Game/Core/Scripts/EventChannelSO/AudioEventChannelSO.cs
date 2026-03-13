using UnityEngine.Events;
using UnityEngine;

/// <summary>
/// This class is used for Events that have a audioclip argument.
/// support randomization
/// </summary>

[CreateAssetMenu(menuName = "Events/Audio Event Channel")]
public class AudioEventChannelSO : DescriptionBaseSO
{
	public event UnityAction<AudioGroupSO> OnEventRaised;

	public void RaiseEvent(AudioGroupSO clips)
	{
		if (OnEventRaised != null)
			OnEventRaised.Invoke(clips);
	}
}
