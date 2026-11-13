namespace BudgetPipeline;

public sealed record ExpenseRequest(int Id, string Category, decimal Amount);

public sealed class ValidatedExpense
{
    public int Id { get; }
    public string Category { get; }
    public decimal Amount { get; }

    private ValidatedExpense(int id, string category, decimal amount)
    {
        Id = id;
        Category = category;
        Amount = amount;
    }

    /// <summary>Short-circuiting A7-style validation: returns the first structured failure.</summary>
    public static Result<ValidatedExpense> CreateResult(
        ExpenseRequest? request, BudgetLimits limits, Ledger ledger)
    {
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(ledger);

        return ExpenseRules.RequireRequest(request)
            .Bind(ExpenseRules.CheckId)
            .Bind(ExpenseRules.CheckAmount)
            .Bind(r => ExpenseRules.FindRoom(r, limits, ledger))
            .Bind(ExpenseRules.CheckBudget)
            .Map(room => new ValidatedExpense(room.Request.Id, room.Category, room.Request.Amount));
    }

    /// <summary>A6-style validation: collects all applicable human-readable reasons at once.</summary>
    public static ExpenseValidation Create(ExpenseRequest? request, BudgetLimits limits, Ledger ledger)
    {
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(ledger);

        if (request is null)
            return new ExpenseValidation.Invalid(new[] { "Request is missing." });

        var reasons = new List<string>();
        var category = (request.Category ?? string.Empty).Trim();

        if (request.Id <= 0)
            reasons.Add("Id must be positive.");

        if (request.Amount <= 0)
            reasons.Add("Amount must be greater than zero.");

        if (category.Length == 0)
            reasons.Add("Category is required.");
        else if (!ledger.TryGetRemaining(category, limits, out var remaining))
            reasons.Add($"Category '{category}' does not exist.");
        else if (request.Amount > remaining)
            reasons.Add($"Amount {Format.Money(request.Amount)} exceeds the remaining budget " +
                        $"{Format.Money(remaining)} for '{category}'.");

        if (reasons.Count > 0)
            return new ExpenseValidation.Invalid(reasons.ToArray());

        return new ExpenseValidation.Valid(new ValidatedExpense(request.Id, category, request.Amount));
    }
}

public abstract record ExpenseValidation
{
    private ExpenseValidation() { }

    public sealed record Valid(ValidatedExpense Expense) : ExpenseValidation;

    public sealed record Invalid(IReadOnlyList<string> Reasons) : ExpenseValidation;
}

public abstract record ExpenseState
{
    private ExpenseState() { }

    public sealed record Pending(ValidatedExpense Expense) : ExpenseState;

    public sealed record Approved(ValidatedExpense Expense, decimal RemainingAfter) : ExpenseState;

    public sealed record Rejected(IReadOnlyList<string> Reasons) : ExpenseState;
}

public sealed record TransitionResult(ExpenseState State, Ledger Ledger, string Message);

internal static class Format
{
    public static string Money(decimal value) =>
        value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
}
