namespace BudgetPipeline;

public sealed record CategorySpendingReport(
    string Category,
    int TransactionCount,
    decimal TotalAmount);

public sealed record ScoredTransactionReport(
    int Id,
    string Category,
    decimal Amount,
    TransactionStatus Status,
    decimal Score);

public static class BudgetReports
{
    public static IReadOnlyList<CategorySpendingReport> BuildCategorySpendingReport(
        IEnumerable<Transaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        var source = transactions.ToArray();

        return source.WithStatus(TransactionStatus.Approved)
            .Concat(source.WithStatus(TransactionStatus.Pending))
            .Where(transaction => transaction.Amount > 0)
            .GroupBy(transaction => transaction.Category, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Any(transaction => transaction.Status == TransactionStatus.Approved))
            .Select(group => new CategorySpendingReport(
                group.Key,
                group.Count(),
                group.Sum(transaction => transaction.Amount)))
            .OrderByDescending(report => report.TotalAmount)
            .ThenBy(report => report.Category, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<ScoredTransactionReport> BuildScoredTransactionsReport(
        IEnumerable<Transaction> transactions,
        decimal minimumAmount,
        Func<Transaction, decimal> scoreFunction)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        ArgumentNullException.ThrowIfNull(scoreFunction);

        if (minimumAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumAmount));

        return transactions
            .Where(transaction => transaction.Status != TransactionStatus.Rejected
                                  && transaction.Amount >= minimumAmount)
            .Select(transaction => new ScoredTransactionReport(
                transaction.Id,
                transaction.Category,
                transaction.Amount,
                transaction.Status,
                scoreFunction(transaction)))
            .OrderByDescending(report => report.Score)
            .ThenBy(report => report.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(report => report.Id)
            .ToArray();
    }
}
