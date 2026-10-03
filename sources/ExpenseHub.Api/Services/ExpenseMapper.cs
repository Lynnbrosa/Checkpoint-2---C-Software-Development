using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Services;

internal static class ExpenseMapper
{
    public static ExpenseResponse ToResponse(this Expense expense) => new()
    {
        Id = expense.Id,
        OwnerId = expense.OwnerId,
        CategoryId = expense.CategoryId,
        CategoryName = expense.Category?.Name,
        Description = expense.Description,
        Amount = expense.Amount,
        ExpenseDate = expense.ExpenseDate,
        Status = expense.Status,
        RejectionReason = expense.RejectionReason,
        Payment = expense.Payment is null
            ? null
            : new PaymentResponse
            {
                Amount = expense.Payment.Amount,
                PaidById = expense.Payment.PaidById,
                PaidAt = expense.Payment.PaidAt,
            },
        CreatedAt = expense.CreatedAt,
        UpdatedAt = expense.UpdatedAt,
    };
}
