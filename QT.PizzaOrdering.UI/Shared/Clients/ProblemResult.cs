using QT.PizzaOrdering.Core.Shared;

using System.Net;
using System.Text.Json;

namespace QT.PizzaOrdering.UI.Shared.Clients;

/// <summary>
/// Builds a failed <see cref="ClientResult{T}"/> from a non-success API response so callers see the
/// server's actual error instead of a generic status message. Problem details are carried onto
/// <see cref="ClientError"/> field by field; anything else falls back to the status line. An expected
/// API failure, not an exceptional one - returned directly rather than thrown.
/// </summary>
internal static class ProblemResult
{
    public static async Task<ClientResult<T>> FromResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        string fallback = $"Request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}).";
        string body = await response.Content.ReadAsStringAsync(ct);
        return ClientResult<T>.Fail(Parse(body, response.StatusCode, fallback));
    }

    private static ClientError Parse(string body, HttpStatusCode status, string fallback)
    {
        if (string.IsNullOrWhiteSpace(body))
            return new ClientError(fallback, status);

        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? FromProblemDetails(document.RootElement, status, fallback)
                : new ClientError(fallback, status);
        }
        catch (JsonException)
        {
            return new ClientError(fallback, status);
        }
    }

    // Message precedence: every validation message joined, else the detail, else the title, else the status line.
    private static ClientError FromProblemDetails(JsonElement root, HttpStatusCode status, string fallback)
    {
        string? title = StringMember(root, "title");
        string? detail = StringMember(root, "detail");
        IReadOnlyDictionary<string, string[]>? errors = ErrorsMember(root);

        string? joined = errors is null ? null : string.Join(" ", errors.Values.SelectMany(messages => messages));
        string message = FirstNonBlank(joined, detail, title) ?? fallback;
        return new ClientError(message, status, title, detail, errors);
    }

    private static string? StringMember(JsonElement root, string name) =>
        root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static IReadOnlyDictionary<string, string[]>? ErrorsMember(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out JsonElement errors) || errors.ValueKind != JsonValueKind.Object)
            return null;

        return errors.EnumerateObject().ToDictionary(
            field => field.Name,
            field => field.Value.ValueKind == JsonValueKind.Array
                ? field.Value.EnumerateArray().Select(message => message.GetString()).OfType<string>().ToArray()
                : []);
    }

    private static string? FirstNonBlank(params string?[] candidates) =>
        candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));
}
