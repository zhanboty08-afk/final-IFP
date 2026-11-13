using BudgetPipeline;

var tests = new (string Name, Action Run)[]
{
    ("Normal workflow", NormalWorkflow),
    ("Empty dataset", EmptyDataset),
    ("Boundary value zero", BoundaryValue),
    ("Invalid and missing input", InvalidAndMissingInput),
    ("Two policies produce different results", TwoPolicies),
    ("Stages work and input stays unchanged", PurityAndStages),
    ("Invalid negative amount is rejected", NegativeAmountRejected),
    ("Header-only file is a valid empty dataset", HeaderOnlyIsEmpty),
    ("Numeric status is rejected", NumericStatusRejected),
    ("Category names ignore letter case", CategoryCaseInsensitive),
    ("A6 factory success", FactorySuccess),
    ("A6 factory rejects every rule with a reason", FactoryRejection),
    ("A6 boundary: exactly the remaining budget", FactoryBoundary),
    ("A6 distinct cases and pattern matching", DistinctCases),
    ("A6 normal operation: submit then approve", NormalOperation),
    ("A6 missing prerequisite is rejected", MissingPrerequisite),
    ("A6 repeated approval follows the rule", RepeatedApproval),
    ("A6 failed transition preserves state", StatePreservation),
    ("A6 request parser", RequestParser),
    ("A6 ledger is seeded from the A5 pipeline", LedgerFromPipeline),
    ("A7 Result has two distinct cases and rejects null", Assignment7Tests.ResultCases),
    ("A7 Map success", Assignment7Tests.MapSuccess),
    ("A7 Bind success", Assignment7Tests.BindSuccess),
    ("A7 Map and Bind skip their function after a failure", Assignment7Tests.FailurePropagation),
    ("A7 complete success of the Result chain", Assignment7Tests.CompleteSuccess),
    ("A7 domain failures with useful errors", Assignment7Tests.DomainFailures),
    ("A7 missing request and invalid id", Assignment7Tests.MissingRequestAndId),
    ("A7 first failure stops the chain", Assignment7Tests.FirstFailureWins),
    ("A7 operation not allowed in the current state", Assignment7Tests.StateNotAllowed),
    ("A7 approval checks the budget again", Assignment7Tests.ApprovalChecksBudgetAgain),
    ("A7 lookup present and absent", Assignment7Tests.LookupPresentAndAbsent),
    ("A7 zero, false and empty text are values, not absence", Assignment7Tests.ZeroFalseAndEmptyAreValues),
    ("A7 missing file keeps the ledger", Assignment7Tests.MissingFile),
    ("A7 malformed files become failures, no partial update", Assignment7Tests.MalformedFiles),
    ("A7 valid files load through the Map chain", Assignment7Tests.ValidFilesLoad),
    ("A8 status extension filters transactions", Assignment8Tests.StatusExtension),
    ("A8 category totals extension groups and sums", Assignment8Tests.CategoryTotalsExtension),
    ("A8 category spending report normal result", Assignment8Tests.CategoryReportNormal),
    ("A8 reports handle empty input", Assignment8Tests.EmptyReports),
    ("A8 reports handle no matching records", Assignment8Tests.NoMatchingReports),
    ("A8 sorting is deterministic for tied scores", Assignment8Tests.DeterministicTies),
    ("A8 scored report filters and projects records", Assignment8Tests.ScoredReportFiltersAndProjects),
    ("A8 two curried configurations produce different scores", Assignment8Tests.CurriedConfigurations),
    ("A8 deferred workflow does not run before enumeration", Assignment8Tests.DeferredDoesNotRunBeforeEnumeration),
    ("A8 deferred workflow processes during iteration", Assignment8Tests.DeferredRunsDuringIteration),
    ("A8 materialized snapshot can be reused", Assignment8Tests.MaterializedSnapshotIsReusable),
    ("Report export saves valid CSV file", Assignment8Tests.ReportExportCreatesValidCsv),
    ("A8 file is released after loading", Assignment8Tests.FileCanBeChangedAfterLoad),
    ("Native C utilization call returns a real result", NativeUtilizationCall),
    ("Native C rejects an invalid budget limit explicitly", NativeInvalidLimitIsControlled)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS: {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL: {test.Name} — {ex.Message}");
    }
}

Console.WriteLine($"\nTests: {tests.Length - failures} passed, {failures} failed.");
if (failures > 0)
    Environment.ExitCode = 1;

static Transaction T(int id, string category, decimal amount, TransactionStatus status) =>
    new(id, category, amount, status);

static void NativeUtilizationCall()
{
    var result = NativeBudgetAdapter.UtilizationPercent(15m, 20m);
    Assert.True(result is Result<decimal>.Ok);
    Assert.Equal(75m, ((Result<decimal>.Ok)result).Value);
}

static void NativeInvalidLimitIsControlled()
{
    var result = NativeBudgetAdapter.UtilizationPercent(15m, 0m);
    Assert.True(result is Result<decimal>.Fail);
    Assert.Equal(ErrorCodes.NativeIntegration, ((Result<decimal>.Fail)result).Error.Code);
}

static void NormalWorkflow()
{
    var input = new[]
    {
        T(1, "Food", 12.50m, TransactionStatus.Approved),
        T(2, "Transport", 3m, TransactionStatus.Approved),
        T(3, "Books", 20m, TransactionStatus.Pending)
    };

    var result = BudgetWorkflow.Run(input, BudgetWorkflow.IncludeApprovedOnly);
    Assert.Equal(2, result.TransactionCount);
    Assert.Equal(15.50m, result.TotalAmount);
    Assert.Equal(2, result.CategoryTotals.Count);
    Assert.Equal("Food", result.CategoryTotals[0].Category);
    Assert.Equal(12.50m, result.CategoryTotals[0].Total);
}

static void EmptyDataset()
{
    var result = BudgetWorkflow.Run(Array.Empty<Transaction>(), BudgetWorkflow.IncludeApprovedOnly);
    Assert.Equal(0, result.TransactionCount);
    Assert.Equal(0m, result.TotalAmount);
    Assert.Equal(0, result.CategoryTotals.Count);
}

static void BoundaryValue()
{
    var input = new[] { T(1, "Food", 0m, TransactionStatus.Approved) };
    var result = BudgetWorkflow.Run(input, BudgetWorkflow.IncludeApprovedOnly);
    Assert.Equal(1, result.TransactionCount);
    Assert.Equal(0m, result.TotalAmount);
}

static void InvalidAndMissingInput()
{
    Assert.Throws<ArgumentNullException>(() => TransactionParser.ParseLines(null));
    Assert.Throws<FormatException>(() => TransactionParser.ParseLines(new[]
    {
        "Id,Category,Amount,Status",
        "abc,Food,10,Approved"
    }));
}

static void TwoPolicies()
{
    var input = new[]
    {
        T(1, "Food", 10m, TransactionStatus.Approved),
        T(2, "Books", 20m, TransactionStatus.Pending),
        T(3, "Transport", 5m, TransactionStatus.Rejected)
    };

    var approved = BudgetWorkflow.Run(input, BudgetWorkflow.IncludeApprovedOnly);
    var approvedAndPending = BudgetWorkflow.Run(input, BudgetWorkflow.IncludeApprovedAndPending);

    Assert.Equal(10m, approved.TotalAmount);
    Assert.Equal(30m, approvedAndPending.TotalAmount);
    Assert.True(approved.TotalAmount != approvedAndPending.TotalAmount);
}

static void PurityAndStages()
{
    var input = new[]
    {
        T(1, "Food", 4m, TransactionStatus.Approved),
        T(2, "Food", 6m, TransactionStatus.Pending)
    };
    var snapshot = input.ToArray();

    var selected = BudgetWorkflow.SelectTransactions(input, BudgetWorkflow.IncludeApprovedOnly);
    Assert.Equal(1, selected.Count);
    Assert.True(!ReferenceEquals(input, selected));

    var totals = BudgetWorkflow.CalculateCategoryTotals(selected);
    Assert.Equal(4m, totals.Single().Total);

    var summary = BudgetWorkflow.CreateSummary(selected, totals);
    Assert.Equal(4m, summary.TotalAmount);

    var callerTotals = totals.ToArray();
    var summary2 = BudgetWorkflow.CreateSummary(selected, callerTotals);
    callerTotals[0] = new CategoryTotal("Changed", 999m);
    Assert.Equal("Food", summary2.CategoryTotals[0].Category);

    var first = BudgetWorkflow.Run(input, BudgetWorkflow.IncludeApprovedAndPending);
    var second = BudgetWorkflow.Run(input, BudgetWorkflow.IncludeApprovedAndPending);
    Assert.Equal(first.TotalAmount, second.TotalAmount);

    Assert.Equal(snapshot.Length, input.Length);
    for (var i = 0; i < input.Length; i++)
        Assert.Equal(snapshot[i], input[i]);
}

static void NegativeAmountRejected()
{
    Assert.Throws<FormatException>(() => TransactionParser.ParseLines(new[]
    {
        "Id,Category,Amount,Status",
        "1,Food,-1,Approved"
    }));
}

static void HeaderOnlyIsEmpty()
{
    var parsed = TransactionParser.ParseLines(new[] { "Id,Category,Amount,Status" });
    Assert.Equal(0, parsed.Count);

    var result = BudgetWorkflow.Run(parsed, BudgetWorkflow.IncludeApprovedOnly);
    Assert.Equal(0, result.TransactionCount);
    Assert.Equal(0m, result.TotalAmount);

    Assert.Throws<ArgumentException>(() => TransactionParser.ParseLines(Array.Empty<string>()));
}

static void NumericStatusRejected()
{
    Assert.Throws<FormatException>(() => TransactionParser.ParseLines(new[]
    {
        "Id,Category,Amount,Status",
        "1,Food,5,1"
    }));

    var parsed = TransactionParser.ParseLines(new[]
    {
        "Id,Category,Amount,Status",
        "1,Food,5,approved"
    });
    Assert.Equal(TransactionStatus.Approved, parsed[0].Status);
}

static void CategoryCaseInsensitive()
{
    var input = new[]
    {
        T(1, "Food", 5m, TransactionStatus.Approved),
        T(2, "food", 7m, TransactionStatus.Approved)
    };
    var result = BudgetWorkflow.Run(input, BudgetWorkflow.IncludeApprovedOnly);
    Assert.Equal(1, result.CategoryTotals.Count);
    Assert.Equal(12m, result.CategoryTotals[0].Total);
}

static BudgetLimits Limits() => new BudgetLimits(new Dictionary<string, decimal>
{
    ["Food"] = 20m,
    ["Transport"] = 10m,
    ["Books"] = 30m
});

static Ledger SeedLedger() => Ledger.FromTotals(new[]
{
    new CategoryTotal("Food", 12.50m),
    new CategoryTotal("Transport", 3m)
});

static ExpenseRequest R(int id, string category, decimal amount) => new ExpenseRequest(id, category, amount);

static ValidatedExpense MustBeValid(ExpenseValidation validation) => validation switch
{
    ExpenseValidation.Valid ok => ok.Expense,
    _ => throw new Exception("Expected a Valid outcome.")
};

static IReadOnlyList<string> MustBeInvalid(ExpenseValidation validation) => validation switch
{
    ExpenseValidation.Invalid bad => bad.Reasons,
    _ => throw new Exception("Expected an Invalid outcome.")
};

static void FactorySuccess()
{
    var expense = MustBeValid(ValidatedExpense.Create(R(1, " Food ", 5m), Limits(), SeedLedger()));
    Assert.Equal(1, expense.Id);
    Assert.Equal("Food", expense.Category);
    Assert.Equal(5m, expense.Amount);
}

static void FactoryRejection()
{
    var limits = Limits();
    var ledger = SeedLedger();

    var zero = MustBeInvalid(ValidatedExpense.Create(R(1, "Food", 0m), limits, ledger));
    Assert.Contains("greater than zero", zero[0]);

    var negative = MustBeInvalid(ValidatedExpense.Create(R(1, "Food", -3m), limits, ledger));
    Assert.Contains("greater than zero", negative[0]);

    var unknown = MustBeInvalid(ValidatedExpense.Create(R(1, "Gym", 5m), limits, ledger));
    Assert.Contains("does not exist", unknown[0]);

    var empty = MustBeInvalid(ValidatedExpense.Create(R(1, "  ", 5m), limits, ledger));
    Assert.Contains("Category is required", empty[0]);

    var tooMuch = MustBeInvalid(ValidatedExpense.Create(R(1, "Food", 8m), limits, ledger));
    Assert.Contains("exceeds", tooMuch[0]);

    var badId = MustBeInvalid(ValidatedExpense.Create(R(0, "Food", 5m), limits, ledger));
    Assert.Contains("Id must be positive", badId[0]);

    var missing = MustBeInvalid(ValidatedExpense.Create(null, limits, ledger));
    Assert.Contains("missing", missing[0]);

    var many = MustBeInvalid(ValidatedExpense.Create(R(0, "Gym", -1m), limits, ledger));
    Assert.Equal(3, many.Count);
}

static void FactoryBoundary()
{
    var limits = Limits();
    var ledger = SeedLedger();

    var exact = ValidatedExpense.Create(R(1, "Food", 7.50m), limits, ledger);
    Assert.True(exact is ExpenseValidation.Valid);

    var oneCentOver = ValidatedExpense.Create(R(1, "Food", 7.51m), limits, ledger);
    Assert.True(oneCentOver is ExpenseValidation.Invalid);
}

static void DistinctCases()
{
    var limits = Limits();
    var ledger = SeedLedger();

    Assert.True(ValidatedExpense.Create(R(1, "Food", 1m), limits, ledger) is ExpenseValidation.Valid);
    Assert.True(ValidatedExpense.Create(R(1, "Food", 99m), limits, ledger) is ExpenseValidation.Invalid);

    var pending = ExpenseWorkflow.Submit(R(1, "Food", 1m), limits, ledger);
    var rejected = ExpenseWorkflow.Submit(R(2, "Gym", 1m), limits, ledger);
    var approved = ExpenseWorkflow.Approve(pending, ledger, limits).State;

    Assert.True(pending is ExpenseState.Pending);
    Assert.True(rejected is ExpenseState.Rejected);
    Assert.True(approved is ExpenseState.Approved);

    Assert.Contains("Pending", ExpenseWorkflow.Describe(pending));
    Assert.Contains("Rejected", ExpenseWorkflow.Describe(rejected));
    Assert.Contains("does not exist", ExpenseWorkflow.Describe(rejected));
    Assert.Contains("Approved", ExpenseWorkflow.Describe(approved));
}

static void NormalOperation()
{
    var limits = Limits();
    var ledger = SeedLedger();

    var pending = ExpenseWorkflow.Submit(R(1, "Food", 5m), limits, ledger);
    Assert.True(pending is ExpenseState.Pending);

    var result = ExpenseWorkflow.Approve(pending, ledger, limits);
    if (result.State is not ExpenseState.Approved approved)
        throw new Exception("Expected the Approved state.");
    Assert.Equal(2.50m, approved.RemainingAfter);
    Assert.Equal(17.50m, result.Ledger.Spent("Food"));
}

static void MissingPrerequisite()
{
    var limits = Limits();
    var ledger = SeedLedger();

    var rejected = ExpenseWorkflow.Submit(R(1, "Gym", 5m), limits, ledger);
    var result = ExpenseWorkflow.Approve(rejected, ledger, limits);

    Assert.True(result.State is ExpenseState.Rejected);
    Assert.True(ReferenceEquals(rejected, result.State));
    Assert.True(ReferenceEquals(ledger, result.Ledger));
    Assert.Contains("cannot be approved", result.Message);

    Assert.Throws<ArgumentNullException>(() => ExpenseWorkflow.Approve(null!, ledger, limits));
}

static void RepeatedApproval()
{
    var limits = Limits();
    var ledger = SeedLedger();

    var pending = ExpenseWorkflow.Submit(R(1, "Food", 5m), limits, ledger);
    var first = ExpenseWorkflow.Approve(pending, ledger, limits);
    var second = ExpenseWorkflow.Approve(first.State, first.Ledger, limits);

    Assert.True(ReferenceEquals(first.State, second.State));
    Assert.True(ReferenceEquals(first.Ledger, second.Ledger));
    Assert.Equal(17.50m, second.Ledger.Spent("Food"));
}

static void StatePreservation()
{
    var limits = Limits();
    var seed = SeedLedger();

    var pendingA = ExpenseWorkflow.Submit(R(1, "Food", 5m), limits, seed);
    var pendingB = ExpenseWorkflow.Submit(R(2, "Food", 5m), limits, seed);

    var afterA = ExpenseWorkflow.Approve(pendingA, seed, limits);
    var afterB = ExpenseWorkflow.Approve(pendingB, afterA.Ledger, limits);

    Assert.True(afterB.State is ExpenseState.Rejected);
    Assert.True(ReferenceEquals(afterA.Ledger, afterB.Ledger));
    Assert.Equal(17.50m, afterB.Ledger.Spent("Food"));

    Assert.Equal(12.50m, seed.Spent("Food"));
    Assert.True(pendingB is ExpenseState.Pending);
}

static void RequestParser()
{
    var parsed = ExpenseRequestParser.ParseLines(new[]
    {
        "Id,Category,Amount",
        "1,Food,5.00",
        "2,Gym,-2"
    });
    Assert.Equal(2, parsed.Count);
    Assert.Equal(R(1, "Food", 5.00m), parsed[0]);
    Assert.Equal(-2m, parsed[1].Amount);

    Assert.Equal(0, ExpenseRequestParser.ParseLines(new[] { "Id,Category,Amount" }).Count);
    Assert.Throws<ArgumentNullException>(() => ExpenseRequestParser.ParseLines(null));
    Assert.Throws<FormatException>(() => ExpenseRequestParser.ParseLines(new[]
    {
        "Id,Category,Amount",
        "1,Food,abc"
    }));
}

static void LedgerFromPipeline()
{
    var transactions = new[]
    {
        T(1, "Food", 12.50m, TransactionStatus.Approved),
        T(2, "Transport", 3m, TransactionStatus.Approved),
        T(3, "Books", 20m, TransactionStatus.Pending)
    };
    var summary = BudgetWorkflow.Run(transactions, BudgetWorkflow.IncludeApprovedOnly);
    var ledger = Ledger.FromTotals(summary.CategoryTotals);

    Assert.Equal(12.50m, ledger.Spent("Food"));
    Assert.Equal(0m, ledger.Spent("Books"));

    Assert.True(ledger.TryGetRemaining("Food", Limits(), out var remaining));
    Assert.Equal(7.50m, remaining);
    Assert.True(!ledger.TryGetRemaining("Gym", Limits(), out _));
}

static class Assert
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected <{expected}> but got <{actual}>.");
    }

    public static void Contains(string expectedPart, string actual)
    {
        if (!actual.Contains(expectedPart, StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Expected text to contain <{expectedPart}> but got <{actual}>.");
    }

    public static void True(bool condition)
    {
        if (!condition)
            throw new Exception("Expected condition to be true.");
    }

    public static void Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new Exception($"Expected {typeof(TException).Name}, got {ex.GetType().Name}.");
        }

        throw new Exception($"Expected {typeof(TException).Name}, but no exception was thrown.");
    }
}
