using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Bookings.Application.Messaging;
using Shared.Contracts;

namespace Bookings.Infrastructure.Messaging;

/// <summary>
/// Продюсер Kafka: один на всё приложение, ключ сообщения = EventId
/// </summary>
internal sealed class KafkaBookingConfirmedPublisher : IBookingConfirmedPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaBookingConfirmedPublisher> _logger;

    public KafkaBookingConfirmedPublisher(
        IConfiguration configuration,
        ILogger<KafkaBookingConfirmedPublisher> logger)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("В конфигурации не задан Kafka:BootstrapServers");

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }


    public async Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(message, JsonOptions);

        // ключ = EventId, чтобы брони одного события шли в один partition по порядку
        var kafkaMessage = new Message<string, string>
        {
            Key = message.EventId.ToString(),
            Value = payload
        };

        var result = await _producer.ProduceAsync(
            KafkaTopics.BookingConfirmed,
            kafkaMessage,
            cancellationToken);

        _logger.LogInformation(
            "Отправлено BookingConfirmed: BookingId={BookingId}, EventId={EventId}, partition={Partition}, offset={Offset}",
            message.BookingId,
            message.EventId,
            result.Partition.Value,
            result.Offset.Value);
    }

    public void Dispose()
    {
        // дожидаемся отправки буфера и закрываем продюсер
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
