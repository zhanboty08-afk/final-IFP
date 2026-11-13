namespace BudgetPipeline;

public static class TransactionScoring
{
    // Each call fixes one argument and returns the function for the next argument.
    public static Func<decimal, Func<Transaction, decimal>> CreateScore(decimal amountWeight)
    {
        if (amountWeight < 0)
            throw new ArgumentOutOfRangeException(nameof(amountWeight));

        return pendingBonus =>
        {
            if (pendingBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(pendingBonus));

            return transaction =>
            {
                ArgumentNullException.ThrowIfNull(transaction);
                var bonus = transaction.Status == TransactionStatus.Pending ? pendingBonus : 0m;
                return decimal.Round(transaction.Amount * amountWeight + bonus, 2,
                    MidpointRounding.AwayFromZero);
            };
        };
    }
}
