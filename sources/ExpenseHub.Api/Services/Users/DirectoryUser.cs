using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Contracts.Responses;

namespace ExpenseHub.Api.Services.Users;

internal sealed record DirectoryUser(string Id, string Email, string FullName, IReadOnlyList<string> Roles)
{
    public UserResponse ToResponse() => new()
    {
        Id = Id,
        Email = Email,
        FullName = FullName,
        Roles = Roles.Order().ToList(),
    };
}
