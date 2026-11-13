using System.Globalization;

namespace BudgetPipeline;

internal static class LookupDemo
{
    internal static void Run(BudgetSummary approvedSummary, BudgetLimits limits)
    {
        var ledger = Ledger.FromTotals(approvedSummary.CategoryTotals);

        Console.WriteLine();
        Console.WriteLine("Explicit Lookups (Maybe<T>)");
        Console.WriteLine("---------------------------");
        foreach (var category in new[] { "Food", "Gifts", "Gym" })
        {
            var limitText = limits.Find(category).Match(
                limit => $"limit {limit.ToString("0.00", CultureInfo.InvariantCulture)}",
                () => "no such category");
            var spentText = ledger.FindSpent(category).Match(
                spent => $"spent {spent.ToString("0.00", CultureInfo.InvariantCulture)}",
                () => "nothing recorded");
            Console.WriteLine($"  {category}: {limitText}; {spentText}");
        }
    }
}
