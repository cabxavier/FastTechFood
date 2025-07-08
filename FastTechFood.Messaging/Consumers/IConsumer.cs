namespace FastTechFood.Messaging.Consumers
{
    public interface IConsumer<T>
    {
        Task HandleAsync(T message);
    }
}