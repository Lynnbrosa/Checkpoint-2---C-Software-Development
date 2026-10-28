using System;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Messaging;
using ExpenseHub.Api.Services.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExpenseHub.Worker;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddExpenseHubDatabase(builder.Configuration, builder.Environment);
        builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IReportRepository, EfReportRepository>();
        builder.Services.AddScoped<IReportProcessor, ReportProcessor>();
        builder.Services.AddHostedService<ReportConsumerWorker>();

        IHost host = builder.Build();

        await using (AsyncServiceScope scope = host.Services.CreateAsyncScope())
        {
            ExpenseHubDbContext db = scope.ServiceProvider.GetRequiredService<ExpenseHubDbContext>();
            await db.Database.MigrateAsync();
        }

        await host.RunAsync();
    }
}

