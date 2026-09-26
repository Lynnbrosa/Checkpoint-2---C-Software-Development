namespace ExpenseHub.Api.Identity;

internal sealed class AdminSeedOptions
{
    public const string SectionName = "Seed:Admin";

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = "Administrador";

    // vem de user-secrets ou variável de ambiente, nunca do appsettings versionado
    public string? Password { get; set; }
}
