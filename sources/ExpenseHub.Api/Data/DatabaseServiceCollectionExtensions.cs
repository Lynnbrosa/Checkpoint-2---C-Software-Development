using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExpenseHub.Api.Data;

internal static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddExpenseHubDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        string connectionString = configuration.GetConnectionString("ExpenseHub")
            ?? throw new InvalidOperationException("Connection string 'ExpenseHub' não configurada.");

        services.AddDbContext<ExpenseHubDbContext>(options =>
            options.UseSqlite(AnchorToContentRoot(connectionString, environment.ContentRootPath)));

        return services;
    }

    // caminho relativo do sqlite depende de onde o dotnet run foi chamado, então ancora no content root
    private static string AnchorToContentRoot(string connectionString, string contentRootPath)
    {
        SqliteConnectionStringBuilder builder = new(connectionString);
        if (!string.IsNullOrEmpty(builder.DataSource)
            && builder.DataSource != ":memory:"
            && !Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.Combine(contentRootPath, builder.DataSource);
        }

        return builder.ToString();
    }
}
