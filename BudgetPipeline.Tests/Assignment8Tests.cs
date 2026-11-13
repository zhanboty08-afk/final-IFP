using BudgetPipeline;

internal static class Assignment8Tests
{
    private static Transaction T(int id, string category, decimal amount, TransactionStatus status) =>
        new(id, category, amount, status);

    public static void StatusExtension()
    {
        var source = new[]
        {
            T(1, "Food", 5m, TransactionStatus.Approved),
            T(2, "Books", 7m, TransactionStatus.Pending)
        };
        var approved = source.WithStatus(TransactionStatus.Approved).ToArray();
        Assert.Equal(1, approved.Length);
        Assert.Equal(1, approved[0].Id);
        Assert.Throws<ArgumentNullException>(() => TransactionExtensions.WithStatus(null!, TransactionStatus.Approved));
    }

    public static void CategoryTotalsExtension()
    {
        var totals = new[]
        {
            T(1, "Food", 2m, TransactionStatus.Approved),
            T(2, "food", 3m, TransactionStatus.Pending),
            T(3, "Books", 4m, TransactionStatus.Approved)
        }.ToCategoryTotals();
        Assert.Equal(2, totals.Count);
        Assert.Equal("Books", totals[0].Category);
        Assert.Equal(5m, totals.Single(item => item.Category.Equals("Food", StringComparison.OrdinalIgnoreCase)).Total);
    }

    public static void CategoryReportNormal()
    {
        var report = BudgetReports.BuildCategorySpendingReport(Sample());
        Assert.Equal(2, report.Count);
        Assert.Equal("Food", report[0].Category);
        Assert.Equal(2, report[0].TransactionCount);
        Assert.Equal(17.50m, report[0].TotalAmount);
        Assert.Equal("Transport", report[1].Category);
        Assert.Equal(3m, report[1].TotalAmount);
    }

    public static void EmptyReports()
    {
        Assert.Equal(0, BudgetReports.BuildCategorySpendingReport(Array.Empty<Transaction>()).Count);
        Assert.Equal(0, BudgetReports.BuildScoredTransactionsReport(
            Array.Empty<Transaction>(), 0m, TransactionScoring.CreateScore(1m)(0m)).Count);
    }

    public static void NoMatchingReports()
    {
        var rejected = new[] { T(1, "Food", 10m, TransactionStatus.Rejected) };
        Assert.Equal(0, BudgetReports.BuildCategorySpendingReport(rejected).Count);
        Assert.Equal(0, BudgetReports.BuildScoredTransactionsReport(
            rejected, 1m, TransactionScoring.CreateScore(1m)(0m)).Count);
    }

    public static void DeterministicTies()
    {
        var input = new[]
        {
            T(3, "Transport", 5m, TransactionStatus.Approved),
            T(2, "Food", 5m, TransactionStatus.Approved),
            T(1, "Food", 5m, TransactionStatus.Approved)
        };
        var report = BudgetReports.BuildScoredTransactionsReport(
            input, 5m, TransactionScoring.CreateScore(1m)(0m));
        Assert.Equal(1, report[0].Id);
        Assert.Equal(2, report[1].Id);
        Assert.Equal(3, report[2].Id);
    }

    public static void ScoredReportFiltersAndProjects()
    {
        var report = BudgetReports.BuildScoredTransactionsReport(
            Sample(), 5m, TransactionScoring.CreateScore(1m)(0m));
        Assert.Equal(3, report.Count);
        Assert.Equal(20m, report[0].Score);
        Assert.Equal(12.50m, report[1].Score);
        Assert.Equal(5m, report[2].Score);
        Assert.True(report.All(item => item.Status != TransactionStatus.Rejected));
        Assert.Throws<ArgumentOutOfRangeException>(() => BudgetReports.BuildScoredTransactionsReport(
            Sample(), -1m, TransactionScoring.CreateScore(1m)(0m)));
    }

    public static void CurriedConfigurations()
    {
        Func<decimal, Func<decimal, Func<Transaction, decimal>>> createScore =
            TransactionScoring.CreateScore;
        var scoreAtWeightOne = createScore(1m);
        var standard = scoreAtWeightOne(0m);
        var pendingPriority = scoreAtWeightOne(2m);
        var sameTransaction = T(8, "Food", 5m, TransactionStatus.Pending);
        Assert.Equal(5m, standard(sameTransaction));
        Assert.Equal(7m, pendingPriority(sameTransaction));
        Assert.True(standard(sameTransaction) != pendingPriority(sameTransaction));
    }

    public static void DeferredDoesNotRunBeforeEnumeration()
    {
        var enumerations = 0;
        IEnumerable<Transaction> Source()
        {
            enumerations++;
            yield return T(1, "Food", 2m, TransactionStatus.Approved);
            yield return T(2, "Books", 3m, TransactionStatus.Pending);
        }

        var query = TransactionStream.Approved(Source());
        Assert.Equal(0, enumerations);
        var result = query.ToArray();
        Assert.Equal(1, enumerations);
        Assert.Equal(1, result.Length);
    }

    public static void DeferredRunsDuringIteration()
    {
        var processed = 0;
        IEnumerable<Transaction> Source()
        {
            foreach (var item in Sample())
            {
                processed++;
                yield return item;
            }
        }

        var query = TransactionStream.Approved(Source());
        Assert.Equal(0, processed);
        using var iterator = query.GetEnumerator();
        Assert.True(iterator.MoveNext());
        Assert.Equal(1, processed);
    }

    public static void MaterializedSnapshotIsReusable()
    {
        var sourceEnumerations = 0;
        IEnumerable<Transaction> Source()
        {
            sourceEnumerations++;
            foreach (var item in Sample()) yield return item;
        }

        var snapshot = TransactionStream.Approved(Source()).ToArray();
        Assert.Equal(1, sourceEnumerations);
        var firstCount = snapshot.Count();
        var secondCount = snapshot.Count();
        Assert.Equal(firstCount, secondCount);
        Assert.Equal(1, sourceEnumerations);
    }

    public static void ReportExportCreatesValidCsv()
    {
        var folder = Path.Combine(Path.GetTempPath(), "BudgetPipeline-Export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "report.csv");
        try
        {
            var report = BudgetReports.BuildCategorySpendingReport(Sample());
            var result = DataLoader.SaveCategoryReportCsv(path, report);
            Assert.True(result.IsSuccess);
            var lines = File.ReadAllLines(path);
            Assert.Equal(3, lines.Length);
            Assert.Equal("Category,TransactionCount,TotalAmount", lines[0]);
            Assert.Equal("Food,2,17.50", lines[1]);
            Assert.Equal("Transport,1,3.00", lines[2]);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    public static void FileCanBeChangedAfterLoad()
    {
        var folder = Path.Combine(Path.GetTempPath(), "BudgetPipeline-File-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "transactions.csv");
        try
        {
            File.WriteAllLines(path, new[]
            {
                "Id,Category,Amount,Status",
                "1,Food,2.00,Approved"
            });
            Assert.True(DataLoader.LoadTransactions(path).IsSuccess);
            File.WriteAllText(path, "replaced after successful read");
            Assert.Equal("replaced after successful read", File.ReadAllText(path));

            File.WriteAllLines(path, new[] { "Id,Category,Amount,Status", "1,Food,not-a-number,Approved" });
            Assert.True(!DataLoader.LoadTransactions(path).IsSuccess);
            File.WriteAllText(path, "replaced after malformed read");
            Assert.Equal("replaced after malformed read", File.ReadAllText(path));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    private static IReadOnlyList<Transaction> Sample() => new[]
    {
        T(1, "Food", 12.50m, TransactionStatus.Approved),
        T(2, "Transport", 3m, TransactionStatus.Approved),
        T(3, "Food", 0m, TransactionStatus.Approved),
        T(4, "Books", 20m, TransactionStatus.Pending),
        T(5, "Transport", 7.50m, TransactionStatus.Rejected),
        T(6, "Food", 5m, TransactionStatus.Pending)
    };
}
