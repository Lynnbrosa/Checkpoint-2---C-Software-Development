namespace ExpenseHub.Api.Domain;

// tabela de transições do REQUISITOS.md; o que não está aqui é conflito
internal static class ExpenseWorkflow
{
    public static bool TryGetNextStatus(ExpenseStatus current, ExpenseAction action, out ExpenseStatus next)
    {
        ExpenseStatus? target = (current, action) switch
        {
            (ExpenseStatus.Draft, ExpenseAction.Updated) => ExpenseStatus.Draft,
            (ExpenseStatus.Draft, ExpenseAction.Submitted) => ExpenseStatus.Submitted,
            (ExpenseStatus.Submitted, ExpenseAction.Approved) => ExpenseStatus.Approved,
            (ExpenseStatus.Submitted, ExpenseAction.Rejected) => ExpenseStatus.Rejected,
            (ExpenseStatus.Approved, ExpenseAction.Paid) => ExpenseStatus.Paid,
            _ => null,
        };

        next = target.GetValueOrDefault();
        return target.HasValue;
    }
}
