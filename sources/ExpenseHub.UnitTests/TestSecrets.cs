using System;

namespace ExpenseHub.UnitTests;

internal static class TestSecrets
{
    // gerada a cada execução: nenhuma senha literal fica no repositório
    public static string UserSecret { get; } = "Aa1!" + Guid.NewGuid().ToString("N");
}
