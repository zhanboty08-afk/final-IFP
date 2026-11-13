using BudgetPipeline;

static class Assignment7Tests
{
    private static readonly Error Boom = new("BOOM", "something failed", "test");

    private static BudgetLimits Limits() => new BudgetLimits(new Dictionary<string, decimal>
    {
        ["Food"] = 20m,
        ["Transport"] = 10m,
        ["Books"] = 30m,
        ["Gifts"] = 0m
    });

    private static Ledger SeedLedger() => Ledger.FromTotals(new[]
    {
        new CategoryTotal("Food", 12.50m),
        new CategoryTotal("Transport", 3m)
    });

    private static ExpenseRequest R(int id, string category, decimal amount) =>
        new ExpenseRequest(id, category, amount);

    private static T ValueOf<T>(Result<T> result) =>
        result.Match(value => value, error => throw new Exception($"Expected success but got {error}."));

    private static Error ErrorOf<T>(Result<T> result) =>
        result.Match<Error>(_ => throw new Exception("Expected a failure."), error => error);

    private sealed class TempFolder : IDisposable
    {
        public string Path { get; } =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "budget-a7-" + Guid.NewGuid().ToString("N"));

        public TempFolder() => Directory.CreateDirectory(Path);

        public string Write(string name, params string[] lines)
        {
            var file = System.IO.Path.Combine(Path, name);
            File.WriteAllLines(file, lines);
            return file;
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }

    public static void ResultCases()
    {
        var ok = Result<int>.Success(42);
        Assert.True(ok.IsSuccess);
        Assert.Equal(42, ValueOf(ok));

        var failed = Result<int>.Failure(Boom);
        Assert.True(!failed.IsSuccess);
        Assert.Equal("BOOM", ErrorOf(failed).Code);

        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
        Assert.Throws<ArgumentNullException>(() => Result<string>.Failure(null!));
    }

    public static void MapSuccess()
    {
        var received = -1;
        var result = Result<int>.Success(5).Map(value =>
        {
            received = value;
            return value * 2;
        });

        Assert.Equal(5, received);
        Assert.Equal(10, ValueOf(result));
    }

    public static void BindSuccess()
    {
        var received = -1;
        var next = Result<string>.Failure(Boom);

        var result = Result<int>.Success(7).Bind(value =>
        {
            received = value;
            return next;
        });

        Assert.Equal(7, received);
        Assert.Equal("BOOM", ErrorOf(result).Code);

        var chained = Result<int>.Success(7).Bind(value => Result<string>.Success($"v{value}"));
        Assert.Equal("v7", ValueOf(chained));
    }

    public static void FailurePropagation()
    {
        var mapCalls = 0;
        var bindCalls = 0;

        var mapped = Result<int>.Failure(Boom).Map(value =>
        {
            mapCalls++;
            return value + 1;
        });
        var bound = Result<int>.Failure(Boom).Bind(value =>
        {
            bindCalls++;
            return Result<int>.Success(value);
        });

        Assert.Equal(0, mapCalls);
        Assert.Equal(0, bindCalls);
        Assert.Equal(Boom, ErrorOf(mapped));
        Assert.Equal(Boom, ErrorOf(bound));

        var laterCalls = 0;
        var chain = Result<int>.Success(1)
            .Bind(_ => Result<int>.Failure(Boom))
            .Bind(value =>
            {
                laterCalls++;
                return Result<int>.Success(value);
            })
            .Map(value =>
            {
                laterCalls++;
                return value;
            });
        Assert.Equal(0, laterCalls);
        Assert.True(!chain.IsSuccess);
    }

    public static void CompleteSuccess()
    {
        var ledger = SeedLedger();
        var outcome = ValueOf(ExpenseOperations.Process(R(1, " Food ", 5m), Limits(), ledger));

        Assert.Equal(17.50m, outcome.Ledger.Spent("Food"));
        Assert.Contains("Approved: #1 Food 5.00, remaining 2.50", outcome.Text);
        Assert.Equal(12.50m, ledger.Spent("Food"));
    }

    public static void DomainFailures()
    {
        var limits = Limits();
        var ledger = SeedLedger();

        var amount = ErrorOf(ExpenseOperations.Process(R(1, "Food", -4m), limits, ledger));
        Assert.Equal(ErrorCodes.InvalidAmount, amount.Code);
        Assert.Contains("-4.00", amount.Context!);

        var zero = ErrorOf(ExpenseOperations.Process(R(1, "Food", 0m), limits, ledger));
        Assert.Equal(ErrorCodes.InvalidAmount, zero.Code);

        var category = ErrorOf(ExpenseOperations.Process(R(1, "Gym", 5m), limits, ledger));
        Assert.Equal(ErrorCodes.UnknownCategory, category.Code);
        Assert.Contains("Gym", category.Context!);

        var budget = ErrorOf(ExpenseOperations.Process(R(1, "Food", 7.51m), limits, ledger));
        Assert.Equal(ErrorCodes.BudgetExceeded, budget.Code);
        Assert.Contains("remaining 7.50", budget.Context!);
        Assert.Contains("requested 7.51", budget.Context!);

        var exact = ExpenseOperations.Process(R(1, "Food", 7.50m), limits, ledger);
        Assert.True(exact.IsSuccess);
    }

    public static void MissingRequestAndId()
    {
        var limits = Limits();
        var ledger = SeedLedger();

        Assert.Equal(ErrorCodes.MissingRequest, ErrorOf(ExpenseOperations.Process(null, limits, ledger)).Code);
        Assert.Equal(ErrorCodes.InvalidId, ErrorOf(ExpenseOperations.Process(R(0, "Food", 1m), limits, ledger)).Code);
    }

    public static void FirstFailureWins()
    {
        var error = ErrorOf(ExpenseOperations.Process(R(0, "Gym", -1m), Limits(), SeedLedger()));
        Assert.Equal(ErrorCodes.InvalidId, error.Code);

        var categoryFirst = ErrorOf(ExpenseOperations.Process(R(1, "Gym", 5m), Limits(), SeedLedger()));
        Assert.Equal(ErrorCodes.UnknownCategory, categoryFirst.Code);
    }

    public static void StateNotAllowed()
    {
        var limits = Limits();
        var ledger = SeedLedger();

        var rejected = ExpenseWorkflow.Submit(R(1, "Gym", 5m), limits, ledger);
        var error = ErrorOf(ExpenseOperations.Approve(rejected, ledger, limits));
        Assert.Equal(ErrorCodes.InvalidState, error.Code);
        Assert.Contains("Rejected", error.Message);

        var pending = ValueOf(ExpenseOperations.Submit(R(2, "Food", 5m), limits, ledger));
        var first = ValueOf(ExpenseOperations.Approve(pending, ledger, limits));
        var second = ValueOf(ExpenseOperations.Approve(first.State, first.Ledger, limits));
        Assert.True(ReferenceEquals(first.Ledger, second.Ledger));
        Assert.Equal(17.50m, second.Ledger.Spent("Food"));
    }

    public static void ApprovalChecksBudgetAgain()
    {
        var limits = Limits();
        var seed = SeedLedger();

        var pendingA = ValueOf(ExpenseOperations.Submit(R(1, "Food", 5m), limits, seed));
        var pendingB = ValueOf(ExpenseOperations.Submit(R(2, "Food", 5m), limits, seed));

        var afterA = ValueOf(ExpenseOperations.Approve(pendingA, seed, limits));
        var failure = ErrorOf(ExpenseOperations.Approve(pendingB, afterA.Ledger, limits));

        Assert.Equal(ErrorCodes.BudgetExceeded, failure.Code);
        Assert.Equal(17.50m, afterA.Ledger.Spent("Food"));
        Assert.Equal(12.50m, seed.Spent("Food"));
    }

    public static void LookupPresentAndAbsent()
    {
        var limits = Limits();
        var ledger = SeedLedger();

        Assert.True(limits.Find("Food").IsPresent);
        Assert.True(limits.Find(" food ").IsPresent);
        Assert.True(!limits.Find("Gym").IsPresent);
        Assert.True(!limits.Find(null).IsPresent);
        Assert.True(!limits.Find("   ").IsPresent);
        Assert.Equal(20m, limits.Find("Food").Match(limit => limit, () => -1m));

        Assert.Equal(12.50m, ledger.FindSpent("Food").Match(spent => spent, () => -1m));
        Assert.True(!ledger.FindSpent("Books").IsPresent);

        var zeroSpent = Ledger.FromTotals(new[] { new CategoryTotal("Food", 0m) });
        Assert.True(zeroSpent.FindSpent("Food").IsPresent);
        Assert.Equal(0m, zeroSpent.FindSpent("Food").Match(spent => spent, () => -1m));
    }

    public static void ZeroFalseAndEmptyAreValues()
    {
        var limits = Limits();

        var gifts = limits.Find("Gifts");
        Assert.True(gifts.IsPresent);
        Assert.Equal(0m, gifts.Match(limit => limit, () => -1m));

        var error = ErrorOf(ExpenseOperations.Process(R(1, "Gifts", 1m), limits, SeedLedger()));
        Assert.Equal(ErrorCodes.BudgetExceeded, error.Code);

        Assert.True(Maybe.Some(false).IsPresent);
        Assert.True(!Maybe.Some(false).Match(value => value, () => true));
        Assert.True(Maybe.FromNullable<string>("").IsPresent);
        Assert.True(!Maybe.FromNullable<string>(null).IsPresent);
        Assert.Throws<ArgumentNullException>(() => Maybe.Some<string>(null!));

        var converted = Maybe.None<int>().ToResult(Boom);
        Assert.Equal("BOOM", ErrorOf(converted).Code);
        Assert.Equal(3, ValueOf(Maybe.Some(3).ToResult(Boom)));
    }

    public static void MissingFile()
    {
        using var folder = new TempFolder();
        var missing = Path.Combine(folder.Path, "missing.csv");
        var current = SeedLedger();

        var result = DataLoader.LoadLedger(missing);
        var error = ErrorOf(result);
        var after = result.Match(ledger => ledger, _ => current);

        Assert.Equal(ErrorCodes.FileNotFound, error.Code);
        Assert.Contains("missing.csv", error.Context!);
        Assert.True(ReferenceEquals(current, after));
        Assert.Equal(12.50m, after.Spent("Food"));

        Assert.Equal(ErrorCodes.FileNotFound, ErrorOf(DataLoader.LoadRequests(missing)).Code);
        Assert.Equal(ErrorCodes.FileNotFound,
            ErrorOf(DataLoader.LoadTransactions(Path.Combine(folder.Path, "no-folder", "x.csv"))).Code);
        Assert.Equal(ErrorCodes.FileAccess, ErrorOf(DataLoader.LoadTransactions("")).Code);
    }

    public static void MalformedFiles()
    {
        using var folder = new TempFolder();
        var current = SeedLedger();

        var badTransactions = folder.Write("t.csv",
            "Id,Category,Amount,Status", "1,Food,10,Approved", "2,Transport,abc,Approved");
        var result = DataLoader.LoadLedger(badTransactions);
        var error = ErrorOf(result);
        var after = result.Match(ledger => ledger, _ => current);

        Assert.Equal(ErrorCodes.MalformedContent, error.Code);
        Assert.Contains("Line 3", error.Message);
        Assert.Contains("t.csv", error.Context!);
        Assert.True(ReferenceEquals(current, after));

        var badHeader = folder.Write("r.csv", "Id,Category", "1,Food");
        Assert.Equal(ErrorCodes.MalformedContent, ErrorOf(DataLoader.LoadRequests(badHeader)).Code);

        var empty = folder.Write("empty.csv");
        Assert.Equal(ErrorCodes.MalformedContent, ErrorOf(DataLoader.LoadTransactions(empty)).Code);

        var negative = folder.Write("neg.csv", "Id,Category,Amount,Status", "1,Food,-5,Approved");
        Assert.Equal(ErrorCodes.MalformedContent, ErrorOf(DataLoader.LoadTransactions(negative)).Code);
    }

    public static void ValidFilesLoad()
    {
        using var folder = new TempFolder();

        var transactions = folder.Write("t.csv",
            "Id,Category,Amount,Status",
            "1,Food,12.50,Approved",
            "2,Transport,3.00,Approved",
            "3,Books,20.00,Pending");
        var ledger = ValueOf(DataLoader.LoadLedger(transactions));
        Assert.Equal(12.50m, ledger.Spent("Food"));
        Assert.Equal(0m, ledger.Spent("Books"));

        var requests = folder.Write("r.csv", "Id,Category,Amount", "1,Food,5.00", "2,Gym,-2");
        Assert.Equal(2, ValueOf(DataLoader.LoadRequests(requests)).Count);

        var headerOnly = folder.Write("h.csv", "Id,Category,Amount");
        Assert.Equal(0, ValueOf(DataLoader.LoadRequests(headerOnly)).Count);
    }
}
