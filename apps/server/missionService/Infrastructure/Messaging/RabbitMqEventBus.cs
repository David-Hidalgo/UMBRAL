using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using MissionService.Application.Abstractions;

namespace MissionService.Infrastructure.Messaging
{
    public sealed class RabbitMqEventBus : IEventBus, IDisposable
    {
        private readonly RabbitMqOptions _options;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private bool _disposed;

        public RabbitMqEventBus(IOptions<RabbitMqOptions> options)
        {
            _options = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null).GetAwaiter().GetResult();
        }
 
        internal RabbitMqEventBus(IConnection connection, IChannel channel, IOptions<RabbitMqOptions> options)
        {
            _connection = connection;
            _channel = channel;
            _options = options.Value;
        }

        public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var routingKey = typeof(T).Name;
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _channel.Dispose();
            _connection.Dispose();
        }
    }
}
