namespace ExpenseHub.Api.Domain;

/// <summary>
/// Estados de um reembolso. Somente o servidor altera o estado.
/// </summary>
public enum ExpenseStatus
{
    /// <summary>Rascunho, editável pelo proprietário.</summary>
    Draft,

    /// <summary>Enviado e aguardando decisão de um Approver.</summary>
    Submitted,

    /// <summary>Aprovado e aguardando pagamento pelo Finance.</summary>
    Approved,

    /// <summary>Reprovado com justificativa. Estado final.</summary>
    Rejected,

    /// <summary>Pago pelo Finance. Estado final.</summary>
    Paid,
}
