using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseHub.Api.Data;

internal static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        ExpenseHubDbContext context = scope.ServiceProvider.GetRequiredService<ExpenseHubDbContext>();

        // mesmo efeito do "dotnet ef database update", pra quem só quer rodar a api
        await context.Database.MigrateAsync(cancellationToken);

        IdentitySeeder seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
