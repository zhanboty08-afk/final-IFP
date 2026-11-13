namespace BudgetPipeline;

public enum TransactionStatus
{
    Approved,
    Pending,
    Rejected
}

public sealed record Transaction(
    int Id,
    string Category,
    decimal Amount,
    TransactionStatus Status);

public sealed record CategoryTotal(string Category, decimal Total);

public sealed record BudgetSummary(
    int TransactionCount,
    decimal TotalAmount,
    IReadOnlyList<CategoryTotal> CategoryTotals);
