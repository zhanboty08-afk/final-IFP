namespace BudgetPipeline;

public sealed record ExpenseOutcome(Ledger Ledger, string Text);

public static class ExpenseOperations
{
    public static Result<ExpenseState.Pending> Submit(
        ExpenseRequest? request, BudgetLimits limits, Ledger ledger) =>
        ValidatedExpense.CreateResult(request, limits, ledger)
            .Map(expense => new ExpenseState.Pending(expense));

    public static Result<TransitionResult> Approve(
        ExpenseState state, Ledger ledger, BudgetLimits limits)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(limits);

        return state switch
        {
            ExpenseState.Pending pending => ApprovePending(pending, ledger, limits),
            ExpenseState.Approved => Result<TransitionResult>.Success(
                new TransitionResult(state, ledger, "Already approved. Nothing was changed.")),
            ExpenseState.Rejected => Result<TransitionResult>.Failure(
                Errors.InvalidState("Rejected", "approved")),
            _ => throw new InvalidOperationException("Unknown expense state.")
        };
    }

    public static Result<ExpenseOutcome> Process(
        ExpenseRequest? request, BudgetLimits limits, Ledger ledger) =>
        Submit(request, limits, ledger)
            .Bind(pending => Approve(pending, ledger, limits))
            .Map(transition => new ExpenseOutcome(
                transition.Ledger, ExpenseWorkflow.Describe(transition.State)));

    private static Result<TransitionResult> ApprovePending(
        ExpenseState.Pending pending, Ledger ledger, BudgetLimits limits)
    {
        var expense = pending.Expense;
        var request = new ExpenseRequest(expense.Id, expense.Category, expense.Amount);

        return ExpenseRules.FindRoom(request, limits, ledger)
            .Bind(ExpenseRules.CheckBudget)
            .Map(room => new TransitionResult(
                new ExpenseState.Approved(expense, room.Remaining - expense.Amount),
                ledger.WithExpense(expense),
                "Approved."));
    }
}
