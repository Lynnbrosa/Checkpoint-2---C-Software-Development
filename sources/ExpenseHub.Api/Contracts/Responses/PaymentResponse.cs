using System;

namespace ExpenseHub.Api.Contracts.Responses;

/// <summary>
/// Registro do pagamento simulado.
/// </summary>
public sealed class PaymentResponse
{
    /// <summary>Valor pago, igual ao valor aprovado.</summary>
    public required decimal Amount { get; init; }

    /// <summary>Usuário do Finance que registrou o pagamento.</summary>
    public required string PaidById { get; init; }

    /// <summary>Momento do pagamento, em UTC, pelo relógio do servidor.</summary>
    public required DateTime PaidAt { get; init; }
}
