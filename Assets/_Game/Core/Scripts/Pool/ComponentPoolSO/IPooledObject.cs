using ObjectPool;

public interface IPooledObject<T>
{
    void ReturnToPool();
    void AssignPool(IPool<T> pool);
}