namespace Factory
{
    public interface IFactory<T>
    {
        public T Create();
    }
}