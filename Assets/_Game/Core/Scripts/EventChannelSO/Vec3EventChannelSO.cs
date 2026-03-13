using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This class is used for Events that have one Vector3 argument.
/// </summary>

[CreateAssetMenu(menuName = "Events/Vector3 Event Channel")]
public class Vec3EventChannelSO : DescriptionBaseSO
{
    public UnityAction<Vector3> OnEventRaised;

    public void RaiseEvent(Vector3 pos)
    {
        OnEventRaised?.Invoke(pos);
    }
}
