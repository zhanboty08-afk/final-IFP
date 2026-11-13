namespace BudgetPipeline;

internal static class FileFailureDemo
{
    internal static void Run(BudgetSummary approvedSummary, string transactionsPath)
    {
        var folder = Path.GetDirectoryName(transactionsPath) ?? string.Empty;
        var current = Ledger.FromTotals(approvedSummary.CategoryTotals);

        Console.WriteLine();
        Console.WriteLine("Boundary Exception Handling (File Failures)");
        Console.WriteLine("-------------------------------------------");

        var files = new[]
        {
            ("missing file", Path.Combine(folder, "no_such_file.csv")),
            ("malformed content", Path.Combine(folder, "transactions_malformed.csv"))
        };

        foreach (var (title, path) in files)
        {
            var after = DataLoader.LoadLedger(path).Match(
                ledger => ledger,
                error =>
                {
                    Console.WriteLine($"  {title}: FAIL {error}");
                    return current;
                });
            Console.WriteLine($"  ledger unchanged: {ReferenceEquals(current, after)}");
        }

        DataLoader.LoadRequests(Path.Combine(folder, "expense_requests_malformed.csv")).Match(
            list =>
            {
                Console.WriteLine($"  loaded {list.Count} requests");
                return 0;
            },
            error =>
            {
                Console.WriteLine($"  malformed requests file: FAIL {error}");
                return 0;
            });
    }
}
