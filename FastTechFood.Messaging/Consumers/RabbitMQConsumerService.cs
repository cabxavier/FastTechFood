using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace FastTechFood.Messaging.Consumers
{
    public class RabbitMQConsumerService<T> : BackgroundService where T : class
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<RabbitMQConsumerService<T>> logger;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly string queueName;
        private readonly ConnectionFactory factory;

        public RabbitMQConsumerService(IConfiguration configuration, ILogger<RabbitMQConsumerService<T>> logger, IServiceScopeFactory scopeFactory, string queueName)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            this.queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));

            this.factory = new ConnectionFactory
            {
                HostName = this.configuration["RabbitMQ:HostName"] ?? throw new Exception("RabbitMQ:HostName não configurado"),
                UserName = this.configuration["RabbitMQ:UserName"] ?? throw new Exception("RabbitMQ:UserName não configurado"),
                Password = this.configuration["RabbitMQ:Password"] ?? throw new Exception("RabbitMQ:Password não configurado"),
                Port = int.Parse(this.configuration["RabbitMQ:Port"] ?? throw new Exception("RabbitMQ:Port não configurado"))
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.logger.LogInformation($"Iniciando consumo da fila '{this.queueName}'...");

            using var connection = await this.factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: this.queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    this.logger.LogInformation($"Mensagem recebida da fila '{this.queueName}': {json}");

                    using var scope = this.scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IConsumer<T>>();
                    var message = JsonSerializer.Deserialize<T>(json);

                    if (message != null)
                        await handler.HandleAsync(message);
                    else
                        this.logger.LogWarning("Mensagem desserializada como nula.");
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Erro ao processar mensagem da fila '{this.queueName}'.");
                }

                await Task.Yield();
            };

            await channel.BasicConsumeAsync(queue: this.queueName, autoAck: true, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}