using System;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Falha de negócio com mensagem legível.
/// </summary>
public sealed class ServiceError
{
    private ServiceError(ServiceErrorKind kind, string message, string? field, Guid? resourceId = null)
    {
        Kind = kind;
        Message = message;
        Field = field;
        ResourceId = resourceId;
    }

    /// <summary>Tipo da falha.</summary>
    public ServiceErrorKind Kind { get; }

    /// <summary>Mensagem para o cliente.</summary>
    public string Message { get; }

    /// <summary>Campo da requisição relacionado à falha de validação, quando houver.</summary>
    public string? Field { get; }

    /// <summary>Identificador persistido quando a operação registrou o recurso antes da falha.</summary>
    public Guid? ResourceId { get; }

    /// <summary>Entrada inválida.</summary>
    /// <param name="field">Campo inválido.</param>
    /// <param name="message">Motivo.</param>
    /// <returns>Erro de validação.</returns>
    public static ServiceError Validation(string field, string message) => new(ServiceErrorKind.Validation, message, field);

    /// <summary>Recurso inexistente ou invisível para o usuário.</summary>
    /// <param name="message">Motivo.</param>
    /// <returns>Erro de recurso não encontrado.</returns>
    public static ServiceError NotFound(string message) => new(ServiceErrorKind.NotFound, message, null);

    /// <summary>Usuário sem permissão para a operação.</summary>
    /// <param name="message">Motivo.</param>
    /// <returns>Erro de permissão.</returns>
    public static ServiceError Forbidden(string message) => new(ServiceErrorKind.Forbidden, message, null);

    /// <summary>Estado atual não permite a operação.</summary>
    /// <param name="message">Motivo.</param>
    /// <returns>Erro de conflito.</returns>
    public static ServiceError Conflict(string message) => new(ServiceErrorKind.Conflict, message, null);

    /// <summary>Serviço de mensageria indisponível após persistir o recurso.</summary>
    /// <param name="message">Motivo sanitizado.</param>
    /// <param name="resourceId">Identificador persistido antes da falha.</param>
    /// <returns>Erro de indisponibilidade (503).</returns>
    public static ServiceError ServiceUnavailable(string message, Guid? resourceId = null) =>
        new(ServiceErrorKind.ServiceUnavailable, message, null, resourceId);
}
