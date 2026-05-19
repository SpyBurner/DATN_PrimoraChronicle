using Fusion;
using UnityEngine;

//Static instance base class
public abstract class StaticInstance<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake() => Instance = this as T;
    protected virtual void OnApplicationQuit()
    {
        Instance = null;
        Destroy(gameObject);
    }
};

//Singleton, destroy on scene load
public abstract class Singleton<T> : StaticInstance<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // already have a singleton, kill this one
            Destroy(gameObject);
            return;
        }

        // assign before anything else
        base.Awake();
    }
}


//Singleton, stay between scene loads
public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        base.Awake();
        if (Instance == this && transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }
}


/// <summary>
/// Persistent singleton + automatic event subscription.
/// </summary>
public abstract class PersistentEventSingleton<T> : PersistentSingleton<T> where T : MonoBehaviour
{
    protected abstract void SubscribeEvent();
    protected abstract void UnsubscribeEvent();

    protected virtual void OnEnable() => SubscribeEvent();
    protected virtual void OnDisable() => UnsubscribeEvent();
}


/// <summary>
/// Network class of <see cref="StaticInstance{T}"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class StaticNetworkInstance<T> : NetworkBehaviour where T : NetworkBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake() => Instance = this as T;
    protected virtual void OnApplicationQuit()
    {
        Instance = null;
        NetworkRunner.Destroy(gameObject);
    }
};

/// <summary>
/// Network class of <see cref="Singleton{T}"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class NetworkSingleton<T> : StaticNetworkInstance<T> where T : NetworkBehaviour
{
    protected override void Awake()
    {
        //Destroy self if this is a duplicated instance
        if (Instance != null) NetworkRunner.Destroy(gameObject);
        base.Awake();
    }
}

//Singleton, stay between scene loads
public abstract class PersistentNetworkSingleton<T> : NetworkSingleton<T> where T : NetworkBehaviour
{
    protected override void Awake()
    {
        base.Awake();
        if (transform.parent == null) NetworkRunner.DontDestroyOnLoad(gameObject);
    }
}