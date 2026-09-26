using System;
using System.Diagnostics.CodeAnalysis;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Resultado de uma operação de serviço: o valor em caso de sucesso ou o erro de negócio.
/// </summary>
/// <typeparam name="T">Tipo do valor devolvido no sucesso.</typeparam>
public sealed class ServiceResult<T>
    where T : notnull
{
    /// <summary>Resultado de sucesso.</summary>
    /// <param name="value">Valor produzido pela operação.</param>
    public ServiceResult(T value)
    {
        Value = value;
        Succeeded = true;
    }

    /// <summary>Resultado de falha.</summary>
    /// <param name="error">Erro de negócio.</param>
    public ServiceResult(ServiceError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    /// <summary>Indica se a operação terminou sem erro.</summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool Succeeded { get; }

    /// <summary>Valor produzido quando <see cref="Succeeded"/> é verdadeiro.</summary>
    public T? Value { get; }

    /// <summary>Erro quando <see cref="Succeeded"/> é falso.</summary>
    public ServiceError? Error { get; }
}
