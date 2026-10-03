using System.Collections.Generic;
using System.Text.Json;

namespace ExpenseHub.Api.Domain;

// alterações de rascunho viram um json curto na coluna Changes do histórico
internal static class ExpenseChangeLog
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize(IReadOnlyList<ExpenseFieldChange> changes) => JsonSerializer.Serialize(changes, _jsonOptions);

    public static IReadOnlyList<ExpenseFieldChange> Deserialize(string? changes) =>
        string.IsNullOrEmpty(changes)
            ? []
            : JsonSerializer.Deserialize<List<ExpenseFieldChange>>(changes, _jsonOptions) ?? [];
}
