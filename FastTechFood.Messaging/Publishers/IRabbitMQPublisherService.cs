using Microsoft.Extensions.Configuration;

namespace FastTechFood.Messaging.Publishers
{
    public interface IRabbitMQPublisherService
    {
        Task SendMessageAsync<T>(T objeto, string queueName);
        IConfiguration GetConfiguration();
    }
}