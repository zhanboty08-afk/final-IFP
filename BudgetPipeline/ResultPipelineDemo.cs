namespace BudgetPipeline;

internal static class ResultPipelineDemo
{
    internal static void Run(
        IReadOnlyList<ExpenseRequest> requests, BudgetSummary approvedSummary, BudgetLimits limits)
    {
        var ledger = Ledger.FromTotals(approvedSummary.CategoryTotals);

        Console.WriteLine();
        Console.WriteLine("Workflow 3 - Monadic Result Pipeline (Bind / Map)");
        Console.WriteLine("-------------------------------------------------");
        foreach (var request in requests)
        {
            var before = ledger;
            ledger = ExpenseOperations.Process(request, limits, before).Match(
                outcome =>
                {
                    Console.WriteLine($"  Request {request.Id}: OK   {outcome.Text}");
                    return outcome.Ledger;
                },
                error =>
                {
                    Console.WriteLine($"  Request {request.Id}: FAIL {error}");
                    return before;
                });
        }

        Console.WriteLine();
        Console.WriteLine("Invalid State Transition via Result");
        Console.WriteLine("-----------------------------------");
        var rejected = ExpenseWorkflow.Submit(
            new ExpenseRequest(int.MaxValue, "Unknown category", 1m), limits, ledger);
        ExpenseOperations.Approve(rejected, ledger, limits).Match(
            transition =>
            {
                Console.WriteLine($"  OK   {transition.Message}");
                return 0;
            },
            error =>
            {
                Console.WriteLine($"  FAIL {error}");
                return 0;
            });
    }
}
