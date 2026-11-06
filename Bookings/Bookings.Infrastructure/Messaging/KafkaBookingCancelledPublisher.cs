using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Bookings.Application.Messaging;
using Shared.Contracts;

namespace Bookings.Infrastructure.Messaging;

/// <summary>
/// Продюсер Kafka для BookingCancelled; ключ = EventId
/// </summary>
internal sealed class KafkaBookingCancelledPublisher : IBookingCancelledPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaBookingCancelledPublisher> _logger;

    public KafkaBookingCancelledPublisher(
        IConfiguration configuration,
        ILogger<KafkaBookingCancelledPublisher> logger)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("В конфигурации не задан Kafka:BootstrapServers");

        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All
        }).Build();
    }

    public async Task PublishAsync(BookingCancelled message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(message, JsonOptions);

        var result = await _producer.ProduceAsync(
            KafkaTopics.BookingCancelled,
            new Message<string, string>
            {
                Key = message.EventId.ToString(),
                Value = payload
            },
            cancellationToken);

        _logger.LogInformation(
            "Отправлено BookingCancelled: BookingId={BookingId}, EventId={EventId}, partition={Partition}, offset={Offset}",
            message.BookingId,
            message.EventId,
            result.Partition.Value,
            result.Offset.Value);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
