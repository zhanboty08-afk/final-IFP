using System.Globalization;

namespace BudgetPipeline;

internal static class ReportingDemo
{
    internal static void Run(IReadOnlyList<Transaction> transactions, string reportOutputPath)
    {
        Console.WriteLine();
        Console.WriteLine("LINQ Reports & Curried Scoring");
        Console.WriteLine("-----------------------------------");

        var categoryReport = BudgetReports.BuildCategorySpendingReport(transactions);
        Console.WriteLine("Report 1 - Spending for categories with approved activity:");
        foreach (var item in categoryReport)
            Console.WriteLine($"  {item.Category}: {item.TransactionCount} transaction(s), {item.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture)}");

        DataLoader.SaveCategoryReportCsv(reportOutputPath, categoryReport).Match(
            savedPath =>
            {
                Console.WriteLine($"  Exported Report 1 to: {savedPath}");
                return 0;
            },
            error =>
            {
                Console.WriteLine($"  Report export failed: {error}");
                return 0;
            });

        var scoreAtAmountWeightOne = TransactionScoring.CreateScore(1m);
        var standardScore = scoreAtAmountWeightOne(0m);
        var pendingPriorityScore = scoreAtAmountWeightOne(2m);
        var standardReport = BudgetReports.BuildScoredTransactionsReport(transactions, 5m, standardScore);
        var priorityReport = BudgetReports.BuildScoredTransactionsReport(transactions, 5m, pendingPriorityScore);

        Console.WriteLine("Report 2 - Eligible transactions (>= 5.00), standard score:");
        foreach (var item in standardReport)
            Console.WriteLine($"  #{item.Id} {item.Category} {item.Amount.ToString("0.00", CultureInfo.InvariantCulture)} ({item.Status}): score {item.Score.ToString("0.00", CultureInfo.InvariantCulture)}");

        Console.WriteLine("Report 2 - Same data, pending-priority score:");
        foreach (var item in priorityReport)
            Console.WriteLine($"  #{item.Id} {item.Category} {item.Amount.ToString("0.00", CultureInfo.InvariantCulture)} ({item.Status}): score {item.Score.ToString("0.00", CultureInfo.InvariantCulture)}");

        Console.WriteLine();
        Console.WriteLine("Deferred Execution & Materialisation");
        Console.WriteLine("------------------------------------");
        Console.WriteLine("  Before enumeration: the approved-transaction iterator has not run.");
        var deferred = TransactionStream.Approved(transactions);
        var approvedCount = 0;
        Console.WriteLine("  During enumeration:");
        foreach (var transaction in deferred)
        {
            approvedCount++;
            Console.WriteLine($"    yielded transaction #{transaction.Id} ({transaction.Category})");
        }
        Console.WriteLine($"  After enumeration: received {approvedCount} approved transaction(s).");
        var snapshot = deferred.ToArray();
        Console.WriteLine($"  Materialised a reusable snapshot of {snapshot.Length} approved transaction(s).");
    }
}
