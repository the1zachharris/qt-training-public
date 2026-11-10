namespace QT.PizzaOrdering.Core.Shared;

/// <summary>
/// Represents an error with a code and message. The concrete subtype is the error CATEGORY;
/// Code is the specific reason within that category. Subtypes are created through their static
/// factory methods, which own the Code strings.
/// </summary>
public abstract record Error(string Code, string Message)
{
    public sealed record NotFound : Error
    {
        private NotFound(string code, string message) : base(code, message) { }

        /// <summary>The entity of type <paramref name="entity"/> with the given id was not found.</summary>
        public static NotFound For(string entity, object id) =>
            new($"{entity}NotFound", $"{entity} with id '{id}' was not found.");
    }

    public sealed record Validation : Error
    {
        public IReadOnlyDictionary<string, string[]> Failures { get; }

        private Validation(string code, string message, IReadOnlyDictionary<string, string[]> failures)
            : base(code, message) => Failures = failures;

        /// <summary>Field-level failures keyed by field.</summary>
        public static Validation Of(IReadOnlyDictionary<string, string[]> failures) =>
            new("Validation",
                failures.Count == 1 ? $"{failures.Keys.First()} is invalid." : "Multiple fields are invalid.",
                failures);
    }
}
