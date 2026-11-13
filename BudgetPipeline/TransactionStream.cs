namespace BudgetPipeline;

public static class TransactionStream
{
    public static IEnumerable<Transaction> Approved(IEnumerable<Transaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        foreach (var transaction in transactions)
        {
            if (transaction.Status == TransactionStatus.Approved)
                yield return transaction;
        }
    }
}
