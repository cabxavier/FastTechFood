using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FastTechFood.Messaging.Publishers
{
    public class RabbitMQPublisherService : IRabbitMQPublisherService
    {
        public IConfiguration configuration { get; }
        private readonly ConnectionFactory factory;

        public RabbitMQPublisherService(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            this.factory = new ConnectionFactory
            {
                HostName = this.configuration["RabbitMQ:HostName"] ?? throw new Exception("Não foi possível localizar o RabbitMQ:HostName."),
                UserName = this.configuration["RabbitMQ:UserName"] ?? throw new Exception("Não foi possível localizar o RabbitMQ:UserName."),
                Password = this.configuration["RabbitMQ:Password"] ?? throw new Exception("Não foi possível localizar o RabbitMQ:Password."),
                Port = int.Parse(this.configuration["RabbitMQ:Port"] ?? throw new Exception("Não foi possível localizar o RabbitMQ:Port."))
            };
        }

        public async Task SendMessageAsync<T>(T objeto, string queueName)
        {
            try
            {
                if (objeto == null)
                    throw new ArgumentNullException(nameof(objeto));

                if (string.IsNullOrWhiteSpace(queueName))
                    throw new ArgumentException("O nome da fila não pode ser nulo ou vazio.", nameof(queueName));

                using var connection = await this.factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(objeto)));
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao enviar mensagem para a fila '{queueName}'", ex);
            }
        }

        public IConfiguration GetConfiguration()
        {
            return this.configuration ?? throw new ArgumentNullException(nameof(configuration), "A configuração não pode ser nula.");
        }
    }
}