using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseHub.Api.Identity;

internal sealed class IdentitySeeder
{
    private readonly ExpenseHubDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AdminSeedOptions _options;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        ExpenseHubDbContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<AdminSeedOptions> options,
        ILogger<IdentitySeeder> logger)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRolesAsync();
        await EnsureAdminAsync(cancellationToken);
    }

    private async Task EnsureRolesAsync()
    {
        foreach (string role in Roles.All)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                ThrowIfFailed(await _roleManager.CreateAsync(new IdentityRole(role)), $"criar a role {role}");
            }
        }
    }

    private async Task EnsureAdminAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Email))
        {
            throw new InvalidOperationException("Configure Seed:Admin:Email antes de iniciar a API.");
        }

        // se a conta já existe o seed não mexe nela, nem nas roles que o Admin mudou depois
        if (await _userManager.FindByEmailAsync(_options.Email) is not null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException(
                "Configure Seed:Admin:Password com dotnet user-secrets ou com a variável Seed__Admin__Password.");
        }

        ApplicationUser admin = new()
        {
            UserName = _options.Email,
            Email = _options.Email,
            EmailConfirmed = true,
            FullName = _options.FullName,
        };

        // criar o usuário e dar a role na mesma transação, pra não sobrar admin sem role
        await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        ThrowIfFailed(await _userManager.CreateAsync(admin, _options.Password), "criar o Admin inicial");
        ThrowIfFailed(await _userManager.AddToRoleAsync(admin, Roles.Admin), "vincular o Admin inicial à role Admin");
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("Conta Admin inicial criada.");
    }

    private static void ThrowIfFailed(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            string errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Falha ao {operation}: {errors}");
        }
    }
}
