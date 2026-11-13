namespace BudgetPipeline;

public static class ExpenseWorkflow
{
    public static ExpenseState Submit(ExpenseRequest? request, BudgetLimits limits, Ledger ledger)
    {
        var validation = ValidatedExpense.Create(request, limits, ledger);

        return validation switch
        {
            ExpenseValidation.Valid valid => new ExpenseState.Pending(valid.Expense),
            ExpenseValidation.Invalid invalid => new ExpenseState.Rejected(invalid.Reasons),
            _ => throw new InvalidOperationException("Unknown validation outcome.")
        };
    }

    public static TransitionResult Approve(ExpenseState state, Ledger ledger, BudgetLimits limits)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(limits);

        return state switch
        {
            ExpenseState.Pending pending => ApprovePending(pending, ledger, limits),

            ExpenseState.Approved => new TransitionResult(
                state, ledger, "Already approved. Nothing was changed."),

            ExpenseState.Rejected => new TransitionResult(
                state, ledger, "A rejected expense cannot be approved."),

            _ => throw new InvalidOperationException("Unknown expense state.")
        };
    }

    public static string Describe(ExpenseState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state switch
        {
            ExpenseState.Pending p =>
                $"Pending: #{p.Expense.Id} {p.Expense.Category} {Format.Money(p.Expense.Amount)}",
            ExpenseState.Approved a =>
                $"Approved: #{a.Expense.Id} {a.Expense.Category} {Format.Money(a.Expense.Amount)}, " +
                $"remaining {Format.Money(a.RemainingAfter)}",
            ExpenseState.Rejected r =>
                $"Rejected: {string.Join(" ", r.Reasons)}",
            _ => throw new InvalidOperationException("Unknown expense state.")
        };
    }

    private static TransitionResult ApprovePending(
        ExpenseState.Pending pending, Ledger ledger, BudgetLimits limits)
    {
        var expense = pending.Expense;

        if (!ledger.TryGetRemaining(expense.Category, limits, out var remaining)
            || expense.Amount > remaining)
        {
            var reason = $"Budget for '{expense.Category}' no longer covers {Format.Money(expense.Amount)}.";
            return new TransitionResult(
                new ExpenseState.Rejected(new[] { reason }), ledger, reason);
        }

        var newLedger = ledger.WithExpense(expense);
        var approved = new ExpenseState.Approved(expense, remaining - expense.Amount);
        return new TransitionResult(approved, newLedger, "Approved.");
    }
}
