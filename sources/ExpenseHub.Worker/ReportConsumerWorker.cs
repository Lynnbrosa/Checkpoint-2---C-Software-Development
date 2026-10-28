using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Messaging;
using ExpenseHub.Api.Services.Reports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ExpenseHub.Worker;

internal sealed class ReportConsumerWorker : BackgroundService
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<ReportConsumerWorker> _logger;

    public ReportConsumerWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<ReportConsumerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "WorkerStarting queue={QueueName} host={HostName}:{Port}",
                _options.QueueName,
                _options.HostName,
                _options.Port);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeQueueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        ex,
                        "WorkerConnectionRetry queue={QueueName}: aguardando 3s antes de reconectar ao RabbitMQ.",
                        _options.QueueName);
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("WorkerStopped queue={QueueName}", _options.QueueName);
        }
    }

    private async Task ConsumeQueueAsync(CancellationToken stoppingToken)
    {
        ConnectionFactory factory = new()
        {
            HostName = _options.HostName,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.UserName,
            Password = _options.Keyword,
        };

        await using IConnection connection = await factory.CreateConnectionAsync(stoppingToken);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            await HandleMessageAsync(channel, eventArgs, stoppingToken);
        };

        await channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("WorkerListening queue={QueueName}", _options.QueueName);
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken stoppingToken)
    {
        ReportRequestedMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<ReportRequestedMessage>(eventArgs.Body.Span, _jsonOptions);
        }
        catch (JsonException ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "WorkerInvalidPayload deliveryTag={DeliveryTag}: JSON inválido descartado.", eventArgs.DeliveryTag);
            }

            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            return;
        }

        if (message is null || message.ReportId == Guid.Empty)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("WorkerEmptyReportId deliveryTag={DeliveryTag}: mensagem sem reportId válido descartada.", eventArgs.DeliveryTag);
            }

            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            return;
        }

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        IReportProcessor processor = scope.ServiceProvider.GetRequiredService<IReportProcessor>();

        ReportProcessingOutcome outcome = await processor.ProcessAsync(message.ReportId, stoppingToken);
        if (outcome == ReportProcessingOutcome.TransientFailure)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "WorkerMessageRequeued reportId={ReportId} deliveryTag={DeliveryTag}: falha transitória, reencaminhando mensagem.",
                    message.ReportId,
                    eventArgs.DeliveryTag);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            await channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken: stoppingToken);
            return;
        }

        await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "WorkerMessageAcked reportId={ReportId} outcome={Outcome} deliveryTag={DeliveryTag}",
                message.ReportId,
                outcome,
                eventArgs.DeliveryTag);
        }
    }
}
