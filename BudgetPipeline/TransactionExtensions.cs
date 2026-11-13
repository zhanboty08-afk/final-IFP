namespace BudgetPipeline;

public static class TransactionExtensions
{
    public static IEnumerable<Transaction> WithStatus(
        this IEnumerable<Transaction> transactions,
        TransactionStatus status)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        return transactions.Where(transaction => transaction.Status == status);
    }

    public static IReadOnlyList<CategoryTotal> ToCategoryTotals(
        this IEnumerable<Transaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        return transactions
            .GroupBy(transaction => transaction.Category, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CategoryTotal(
                group.Key,
                group.Sum(transaction => transaction.Amount)))
            .OrderBy(total => total.Category, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
