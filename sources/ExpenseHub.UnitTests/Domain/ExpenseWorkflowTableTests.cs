using System;
using System.Collections.Generic;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

// varre todas as combinações estado x ação; qualquer transição nova ou perdida quebra aqui
[TestClass]
internal sealed class ExpenseWorkflowTableTests
{
    private static readonly (ExpenseStatus From, ExpenseAction Action, ExpenseStatus To)[] _contract =
    [
        (ExpenseStatus.Draft, ExpenseAction.Updated, ExpenseStatus.Draft),
        (ExpenseStatus.Draft, ExpenseAction.Submitted, ExpenseStatus.Submitted),
        (ExpenseStatus.Submitted, ExpenseAction.Approved, ExpenseStatus.Approved),
        (ExpenseStatus.Submitted, ExpenseAction.Rejected, ExpenseStatus.Rejected),
        (ExpenseStatus.Approved, ExpenseAction.Paid, ExpenseStatus.Paid),
    ];

    [TestMethod]
    public void TryGetNextStatus_AllowsExactlyTheTransitionsOfTheContract()
    {
        List<(ExpenseStatus From, ExpenseAction Action, ExpenseStatus To)> allowed = [];
        foreach (ExpenseStatus status in Enum.GetValues<ExpenseStatus>())
        {
            foreach (ExpenseAction action in Enum.GetValues<ExpenseAction>())
            {
                if (ExpenseWorkflow.TryGetNextStatus(status, action, out ExpenseStatus next))
                {
                    allowed.Add((status, action, next));
                }
            }
        }

        CollectionAssert.AreEquivalent(_contract, allowed);
    }

    [TestMethod]
    [DataRow(ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Paid)]
    public void TryGetNextStatus_FinalStates_AcceptNoAction(ExpenseStatus finalStatus)
    {
        foreach (ExpenseAction action in Enum.GetValues<ExpenseAction>())
        {
            Assert.IsFalse(
                ExpenseWorkflow.TryGetNextStatus(finalStatus, action, out _),
                $"{finalStatus} não deveria aceitar {action}");
        }
    }

    [TestMethod]
    public void TryGetNextStatus_CreatedIsNeverATransition()
    {
        // criação não parte de estado nenhum; só Expense.CreateDraft registra Created
        foreach (ExpenseStatus status in Enum.GetValues<ExpenseStatus>())
        {
            Assert.IsFalse(ExpenseWorkflow.TryGetNextStatus(status, ExpenseAction.Created, out _));
        }
    }
}
