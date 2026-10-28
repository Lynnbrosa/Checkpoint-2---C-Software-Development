using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpenseHub.Api.Domain;

internal static class ReportResultSerializer
{
    private static readonly JsonSerializerOptions _jsonOptions = CreateOptions();

    public static string Serialize(ReportSummary summary) => JsonSerializer.Serialize(summary, _jsonOptions);

    public static ReportSummary? Deserialize(string? json) =>
        string.IsNullOrEmpty(json)
            ? null
            : JsonSerializer.Deserialize<ReportSummary>(json, _jsonOptions);

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

