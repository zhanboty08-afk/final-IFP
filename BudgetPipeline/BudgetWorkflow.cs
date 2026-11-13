namespace BudgetPipeline;

public static class BudgetWorkflow
{
    public static IReadOnlyList<Transaction> SelectTransactions(
        IEnumerable<Transaction> transactions,
        Func<Transaction, bool> includePolicy)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        ArgumentNullException.ThrowIfNull(includePolicy);

        return transactions.Where(includePolicy).ToArray();
    }

    public static IReadOnlyList<CategoryTotal> CalculateCategoryTotals(
        IEnumerable<Transaction> selectedTransactions)
    {
        ArgumentNullException.ThrowIfNull(selectedTransactions);

        return selectedTransactions.ToCategoryTotals();
    }

    public static BudgetSummary CreateSummary(
        IEnumerable<Transaction> selectedTransactions,
        IReadOnlyList<CategoryTotal> categoryTotals)
    {
        ArgumentNullException.ThrowIfNull(selectedTransactions);
        ArgumentNullException.ThrowIfNull(categoryTotals);

        var records = selectedTransactions.ToArray();
        var totalsCopy = categoryTotals.ToArray();

        return new BudgetSummary(
            records.Length,
            records.Sum(t => t.Amount),
            Array.AsReadOnly(totalsCopy));
    }

    public static BudgetSummary Run(
        IEnumerable<Transaction> transactions,
        Func<Transaction, bool> includePolicy)
    {
        var selected = SelectTransactions(transactions, includePolicy);
        var totals = CalculateCategoryTotals(selected);
        return CreateSummary(selected, totals);
    }

    public static bool IncludeApprovedOnly(Transaction transaction) =>
        transaction.Status == TransactionStatus.Approved;

    public static bool IncludeApprovedAndPending(Transaction transaction) =>
        transaction.Status is TransactionStatus.Approved or TransactionStatus.Pending;
}
