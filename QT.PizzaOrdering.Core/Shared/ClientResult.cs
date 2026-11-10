using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace QT.PizzaOrdering.Core.Shared;

/// <summary>
/// A client-side failure reason. <paramref name="Message"/> is always set and is what a page shows
/// by default. The remaining members carry what the server actually answered, when it answered.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "DTO record - properties set via constructor only.")]
public sealed record ClientError(
    string Message,
    HttpStatusCode? Status = null,
    string? Title = null,
    string? Detail = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);

[ExcludeFromCodeCoverage(Justification = "DTO record - properties set via constructor only.")]
public readonly record struct ClientResult<T>
{
    private ClientResult(bool isSuccess, T? data, ClientError? error)
        => (IsSuccess, Data, Error) = (isSuccess, data, error);

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Data { get; }
    public ClientError? Error { get; }

    public static ClientResult<T> Ok(T data) => new(true, data, null);

    /// <summary>An unexpected/thrown failure (network error, malformed response, ...).</summary>
    public static ClientResult<T> Fail(Exception ex) => new(false, default, new ClientError(ex.Message));

    /// <summary>An expected failure the server reported directly, with everything its response carried.</summary>
    public static ClientResult<T> Fail(ClientError error) => new(false, default, error);
}
