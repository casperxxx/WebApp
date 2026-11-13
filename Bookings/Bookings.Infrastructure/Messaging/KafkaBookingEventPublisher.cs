using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Bookings.Application.Messaging;
using Shared.Contracts;

namespace Bookings.Infrastructure.Messaging;

/// <summary>
/// Продюсер Kafka для booking-events; ключ = EventId
/// </summary>
internal sealed class KafkaBookingEventPublisher : IBookingEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaBookingEventPublisher> _logger;

    public KafkaBookingEventPublisher(
        IConfiguration configuration,
        ILogger<KafkaBookingEventPublisher> logger)
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

    public async Task PublishAsync(BookingLifecycleEvent message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(message, JsonOptions);

        var result = await _producer.ProduceAsync(
            KafkaTopics.BookingEvents,
            new Message<string, string>
            {
                Key = message.EventId.ToString(),
                Value = payload
            },
            cancellationToken);

        _logger.LogInformation(
            "Отправлено {Type}: BookingId={BookingId}, EventId={EventId}, offset={Offset}",
            message.Type,
            message.BookingId,
            message.EventId,
            result.Offset.Value);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
