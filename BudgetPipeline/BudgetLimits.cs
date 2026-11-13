namespace BudgetPipeline;

public sealed class BudgetLimits
{
    private readonly Dictionary<string, decimal> limits;

    public BudgetLimits(IEnumerable<KeyValuePair<string, decimal>> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        limits = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
                throw new ArgumentException("Category name is required.", nameof(source));
            if (pair.Value < 0)
                throw new ArgumentException($"Limit for '{pair.Key}' cannot be negative.", nameof(source));

            limits[pair.Key.Trim()] = pair.Value;
        }
    }

    public bool TryGetLimit(string category, out decimal limit) =>
        limits.TryGetValue(category, out limit);

    public IReadOnlyList<KeyValuePair<string, decimal>> Entries() =>
        limits
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new KeyValuePair<string, decimal>(pair.Key, pair.Value))
            .ToArray();

    public Maybe<decimal> Find(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Maybe.None<decimal>();

        return limits.TryGetValue(category.Trim(), out var limit)
            ? Maybe.Some(limit)
            : Maybe.None<decimal>();
    }
}
