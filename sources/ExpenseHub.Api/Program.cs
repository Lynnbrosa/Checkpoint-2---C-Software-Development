using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using ExpenseHub.Api.Services.Users;
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
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                // campo fora do contrato (ownerId, status, createdAt...) vira 400 em vez de ser ignorado
                options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        builder.Services.AddExpenseHubDatabase(builder.Configuration, builder.Environment);
        builder.Services.AddExpenseHubIdentity(builder.Configuration);

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IExpenseRepository, EfExpenseRepository>();
        builder.Services.AddScoped<IExpenseService, ExpenseService>();
        builder.Services.AddScoped<IUserDirectory, IdentityUserDirectory>();
        builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<IUserAdministrationService, UserAdministrationService>();

        WebApplication app = builder.Build();

        app.UseExceptionHandler();

        // 401 e 403 do pipeline de auth saem sem corpo; isso devolve ProblemDetails nesses casos
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthentication();
        app.UseMiddleware<SecurityStampValidationMiddleware>();
        app.UseAuthorization();

        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("GetHealth");

        await DatabaseInitializer.InitializeAsync(app.Services);
        await app.RunAsync();
    }
}
