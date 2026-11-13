using System.Globalization;

namespace BudgetPipeline;

internal static class BudgetPipelineConsole
{
    internal static BudgetLimits CreateLimits() => new(new Dictionary<string, decimal>
    {
        ["Food"] = 20m,
        ["Transport"] = 10m,
        ["Books"] = 30m,
        ["Gifts"] = 0m
    });

    internal static void Run(
        IReadOnlyList<Transaction> transactions,
        IReadOnlyList<ExpenseRequest> requests,
        string transactionsPath,
        string reportOutputPath)
    {
        var approvedOnly = BudgetWorkflow.Run(transactions, BudgetWorkflow.IncludeApprovedOnly);
        PrintSummary("Workflow 1 - Policy A: Approved only", approvedOnly);
        PrintSummary("Workflow 1 - Policy B: Approved + Pending", BudgetWorkflow.Run(
            transactions, BudgetWorkflow.IncludeApprovedAndPending));

        ReportingDemo.Run(transactions, reportOutputPath);

        var limits = CreateLimits();
        BudgetUtilizationDemo.Run(approvedOnly, limits);
        ExpenseWorkflowDemo.Run(requests, approvedOnly, limits);
        ResultPipelineDemo.Run(requests, approvedOnly, limits);
        LookupDemo.Run(approvedOnly, limits);
        FileFailureDemo.Run(approvedOnly, transactionsPath);
    }

    private static void PrintSummary(string title, BudgetSummary summary)
    {
        Console.WriteLine();
        Console.WriteLine(title);
        Console.WriteLine(new string('-', title.Length));
        Console.WriteLine($"Transactions: {summary.TransactionCount}");
        Console.WriteLine($"Total: {summary.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture)}");
        Console.WriteLine("Category totals:");

        foreach (var category in summary.CategoryTotals)
            Console.WriteLine($"  {category.Category}: {category.Total.ToString("0.00", CultureInfo.InvariantCulture)}");
    }
}
