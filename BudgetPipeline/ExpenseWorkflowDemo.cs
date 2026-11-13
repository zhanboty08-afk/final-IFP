using System.Globalization;

namespace BudgetPipeline;

internal static class ExpenseWorkflowDemo
{
    internal static void Run(
        IReadOnlyList<ExpenseRequest> requests, BudgetSummary approvedSummary, BudgetLimits limits)
    {
        var ledger = Ledger.FromTotals(approvedSummary.CategoryTotals);

        Console.WriteLine();
        Console.WriteLine("Workflow 2 - Expense Requests: Step 1 (Validation)");
        Console.WriteLine("--------------------------------------------------");
        var states = requests
            .Select(request => ExpenseWorkflow.Submit(request, limits, ledger))
            .ToList();
        for (var i = 0; i < requests.Count; i++)
            Console.WriteLine($"  Request {requests[i].Id}: {ExpenseWorkflow.Describe(states[i])}");

        Console.WriteLine();
        Console.WriteLine("Workflow 2 - Expense Requests: Step 2 (Approval in Order)");
        Console.WriteLine("---------------------------------------------------------");
        for (var i = 0; i < states.Count; i++)
        {
            var result = ExpenseWorkflow.Approve(states[i], ledger, limits);
            states[i] = result.State;
            ledger = result.Ledger;
            Console.WriteLine($"  Request {requests[i].Id}: {result.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("Safe Operation Sequence Checks");
        Console.WriteLine("------------------------------");
        var firstApproved = states.FirstOrDefault(state => state is ExpenseState.Approved);
        Console.WriteLine(firstApproved is null
            ? "  Approve again: no expense was approved."
            : $"  Approve again: {ExpenseWorkflow.Approve(firstApproved, ledger, limits).Message}");

        var lastRejected = states.LastOrDefault(state => state is ExpenseState.Rejected);
        Console.WriteLine(lastRejected is null
            ? "  Approve a rejected one: no expense was rejected."
            : $"  Approve a rejected one: {ExpenseWorkflow.Approve(lastRejected, ledger, limits).Message}");

        Console.WriteLine();
        Console.WriteLine("Spent per category after approvals:");
        foreach (var entry in ledger.Entries())
            Console.WriteLine($"  {entry.Category}: {entry.Total.ToString("0.00", CultureInfo.InvariantCulture)}");
    }
}
