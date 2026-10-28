namespace ExpenseHub.Api.Messaging;

internal sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string UserName { get; set; } = "guest";

    public string Keyword { get; set; } = string.Empty;

    public string QueueName { get; set; } = "expensehub.reports";

    public int PublishTimeoutSeconds { get; set; } = 5;
}

