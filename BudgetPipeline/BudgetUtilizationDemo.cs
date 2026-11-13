using System.Globalization;

namespace BudgetPipeline;

internal static class BudgetUtilizationDemo
{
    internal static void Run(BudgetSummary approvedSummary, BudgetLimits limits)
    {
        var ledger = Ledger.FromTotals(approvedSummary.CategoryTotals);

        Console.WriteLine();
        Console.WriteLine("Native C Budget Utilization Report");
        Console.WriteLine("----------------------------------");
        foreach (var (category, limit) in limits.Entries())
        {
            NativeBudgetAdapter.UtilizationPercent(ledger.Spent(category), limit).Match(
                percentage =>
                {
                    Console.WriteLine($"  {category}: {percentage.ToString("0.00", CultureInfo.InvariantCulture)}% used");
                    return 0;
                },
                error =>
                {
                    Console.WriteLine($"  {category}: unavailable ({error})");
                    return 0;
                });
        }
    }
}
