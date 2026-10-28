using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Services.Reports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ExpenseHub.Api.Messaging;

internal sealed class RabbitMqReportMessagePublisher : IReportMessagePublisher
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqReportMessagePublisher> _logger;

    public RabbitMqReportMessagePublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqReportMessagePublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> TryPublishAsync(Guid reportId, CancellationToken cancellationToken)
    {
        try
        {
            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.PublishTimeoutSeconds)));

            ConnectionFactory factory = new()
            {
                HostName = _options.HostName,
                Port = _options.Port,
                VirtualHost = _options.VirtualHost,
                UserName = _options.UserName,
                Password = _options.Keyword,
            };

            await using IConnection connection = await factory.CreateConnectionAsync(timeoutCts.Token);
            CreateChannelOptions channelOptions = new(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);
            await using IChannel channel = await connection.CreateChannelAsync(channelOptions, timeoutCts.Token);

            await channel.QueueDeclareAsync(
                queue: _options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: timeoutCts.Token);

            ReportRequestedMessage payload = new() { ReportId = reportId };
            byte[] body = JsonSerializer.SerializeToUtf8Bytes(payload, _jsonOptions);

            BasicProperties properties = new()
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = reportId.ToString(),
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _options.QueueName,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: timeoutCts.Token);

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    ex,
                    "RabbitMqPublishError reportId={ReportId} queue={QueueName}: falha ao publicar ou confirmar mensagem no broker.",
                    reportId,
                    _options.QueueName);
            }

            return false;
        }
    }
}
