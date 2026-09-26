namespace ExpenseHub.Api.Services;

/// <summary>
/// Tipo de falha de negócio. O controller traduz cada tipo em um status HTTP.
/// </summary>
public enum ServiceErrorKind
{
    /// <summary>Entrada inválida (400).</summary>
    Validation,

    /// <summary>Recurso inexistente ou fora do escopo de leitura (404).</summary>
    NotFound,

    /// <summary>Usuário autenticado sem permissão para a operação (403).</summary>
    Forbidden,

    /// <summary>Operação incompatível com o estado atual ou repetida (409).</summary>
    Conflict,
}
