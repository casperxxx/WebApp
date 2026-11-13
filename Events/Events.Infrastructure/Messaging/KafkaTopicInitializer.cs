using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace Events.Infrastructure.Messaging;

/// <summary>
/// При старте создаёт топик booking-events, если его ещё нет
/// </summary>
internal sealed class KafkaTopicInitializer : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(
        IConfiguration configuration,
        ILogger<KafkaTopicInitializer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(() => EnsureTopicAsync(cancellationToken), CancellationToken.None);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureTopicAsync(CancellationToken cancellationToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _logger.LogWarning("Kafka:BootstrapServers не задан — топик не создаём");
            return;
        }

        for (var attempt = 1; attempt <= 12; attempt++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                using var admin = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = bootstrapServers,
                    SocketTimeoutMs = 5000
                }).Build();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(15));

                await admin.CreateTopicsAsync(
                [
                    new TopicSpecification
                    {
                        Name = KafkaTopics.BookingEvents,
                        NumPartitions = 1,
                        ReplicationFactor = 1
                    }
                ]).WaitAsync(cts.Token);

                _logger.LogInformation("Топик {Topic} создан", KafkaTopics.BookingEvents);
                return;
            }
            catch (CreateTopicsException ex) when (ex.Results.All(r =>
                r.Error.Code == ErrorCode.TopicAlreadyExists || r.Error.Code == ErrorCode.NoError))
            {
                _logger.LogInformation("Топик {Topic} уже есть", KafkaTopics.BookingEvents);
                return;
            }
            catch (Exception ex) when (attempt < 12)
            {
                _logger.LogWarning(ex, "Попытка {Attempt}/12 создать топик, ждём Kafka", attempt);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось создать топик {Topic}", KafkaTopics.BookingEvents);
            }
        }
    }
}
