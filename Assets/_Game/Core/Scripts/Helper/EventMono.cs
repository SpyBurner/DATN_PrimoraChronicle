using UnityEngine;

/// <summary>
/// Base class for MonoBehaviours to enforce subscription and unsubscription to events.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class EventMono : MonoBehaviour
{
    protected abstract void SubscribeEvent();
    protected abstract void UnsubscribeEvent();

    private void OnEnable() => SubscribeEvent();
    private void OnDisable() => UnsubscribeEvent();
}
