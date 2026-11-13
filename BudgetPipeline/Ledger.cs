namespace BudgetPipeline;

public sealed class Ledger
{
    private readonly Dictionary<string, decimal> spent;

    private Ledger(Dictionary<string, decimal> spent)
    {
        this.spent = spent;
    }

    public static Ledger Empty { get; } =
        new Ledger(new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase));

    public static Ledger FromTotals(IEnumerable<CategoryTotal> totals)
    {
        ArgumentNullException.ThrowIfNull(totals);

        var copy = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var total in totals)
            copy[total.Category] = copy.GetValueOrDefault(total.Category) + total.Total;

        return new Ledger(copy);
    }

    public decimal Spent(string category) =>
        spent.TryGetValue(category, out var value) ? value : 0m;

    public Maybe<decimal> FindSpent(string category) =>
        spent.TryGetValue(category, out var value)
            ? Maybe.Some(value)
            : Maybe.None<decimal>();

    public bool TryGetRemaining(string category, BudgetLimits limits, out decimal remaining)
    {
        ArgumentNullException.ThrowIfNull(limits);

        if (limits.TryGetLimit(category, out var limit))
        {
            remaining = limit - Spent(category);
            return true;
        }

        remaining = 0m;
        return false;
    }

    public Ledger WithExpense(ValidatedExpense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);

        var copy = new Dictionary<string, decimal>(spent, StringComparer.OrdinalIgnoreCase);
        copy[expense.Category] = Spent(expense.Category) + expense.Amount;
        return new Ledger(copy);
    }

    public IReadOnlyList<CategoryTotal> Entries() =>
        spent
            .Select(pair => new CategoryTotal(pair.Key, pair.Value))
            .OrderBy(total => total.Category, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
