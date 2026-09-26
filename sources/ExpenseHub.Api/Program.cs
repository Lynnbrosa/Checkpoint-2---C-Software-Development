using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExpenseHub.Api;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        builder.Services.AddExpenseHubDatabase(builder.Configuration, builder.Environment);
        builder.Services.AddExpenseHubIdentity(builder.Configuration);

        WebApplication app = builder.Build();

        app.UseExceptionHandler();

        // 401 e 403 do pipeline de auth saem sem corpo; isso devolve ProblemDetails nesses casos
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("GetHealth");

        await DatabaseInitializer.InitializeAsync(app.Services);
        await app.RunAsync();
    }
}
