namespace ExpenseHub.Api.Domain;

/// <summary>
/// Ações registradas no histórico de um reembolso.
/// </summary>
public enum ExpenseAction
{
    /// <summary>Criação do rascunho.</summary>
    Created,

    /// <summary>Edição do rascunho.</summary>
    Updated,

    /// <summary>Envio do rascunho para aprovação.</summary>
    Submitted,

    /// <summary>Aprovação por um Approver.</summary>
    Approved,

    /// <summary>Reprovação por um Approver.</summary>
    Rejected,

    /// <summary>Registro de pagamento pelo Finance.</summary>
    Paid,
}
