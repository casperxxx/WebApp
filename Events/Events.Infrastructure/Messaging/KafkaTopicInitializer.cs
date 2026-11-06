using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace Events.Infrastructure.Messaging;

/// <summary>
/// При старте создаёт топики booking-confirmed и booking-cancelled, если их ещё нет
/// </summary>
internal sealed class KafkaTopicInitializer : IHostedService
{
    private static readonly string[] Topics =
    [
        KafkaTopics.BookingConfirmed,
        KafkaTopics.BookingCancelled
    ];

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
        // не ждём Kafka в StartAsync — иначе HTTP не поднимется, пока брокер спит
        _ = Task.Run(() => EnsureTopicsAsync(cancellationToken), CancellationToken.None);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureTopicsAsync(CancellationToken cancellationToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _logger.LogWarning("Kafka:BootstrapServers не задан — топики не создаём");
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

                var specs = Topics.Select(name => new TopicSpecification
                {
                    Name = name,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }).ToList();

                await admin.CreateTopicsAsync(specs).WaitAsync(cts.Token);

                _logger.LogInformation("Топики созданы: {Topics}", string.Join(", ", Topics));
                return;
            }
            catch (CreateTopicsException ex) when (ex.Results.All(r =>
                r.Error.Code == ErrorCode.TopicAlreadyExists || r.Error.Code == ErrorCode.NoError))
            {
                _logger.LogInformation("Топики уже есть: {Topics}", string.Join(", ", Topics));
                return;
            }
            catch (Exception ex) when (attempt < 12)
            {
                _logger.LogWarning(
                    ex,
                    "Попытка {Attempt}/12: не удалось создать топики, ждём Kafka",
                    attempt);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось создать топики Kafka");
            }
        }
    }
}
