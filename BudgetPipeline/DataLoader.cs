using System.Globalization;

namespace BudgetPipeline;

public static class DataLoader
{
    public static Result<IReadOnlyList<Transaction>> LoadTransactions(string path) =>
        Load(path, TransactionParser.ParseLines);

    public static Result<IReadOnlyList<ExpenseRequest>> LoadRequests(string path) =>
        Load(path, ExpenseRequestParser.ParseLines);

    public static Result<Ledger> LoadLedger(string transactionsPath) =>
        LoadTransactions(transactionsPath)
            .Map(transactions => BudgetWorkflow.Run(transactions, BudgetWorkflow.IncludeApprovedOnly))
            .Map(summary => Ledger.FromTotals(summary.CategoryTotals));

    public static Result<string> SaveCategoryReportCsv(
        string path, IEnumerable<CategorySpendingReport> report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var lines = new List<string> { "Category,TransactionCount,TotalAmount" };
        foreach (var item in report)
        {
            lines.Add(string.Join(",",
                item.Category,
                item.TransactionCount.ToString(CultureInfo.InvariantCulture),
                item.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture)));
        }

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllLines(path, lines);
            return Result<string>.Success(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                       or ArgumentException or NotSupportedException)
        {
            return Result<string>.Failure(Errors.FileAccess(path, ex.Message));
        }
    }

    private static Result<IReadOnlyList<T>> Load<T>(
        string path, Func<IEnumerable<string>, IReadOnlyList<T>> parse)
    {
        List<string> lines;

        try
        {
            lines = new List<string>();
            using var reader = new StreamReader(path);
            string? line;
            while ((line = reader.ReadLine()) is not null)
                lines.Add(line);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return Result<IReadOnlyList<T>>.Failure(Errors.FileNotFound(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                       or ArgumentException or NotSupportedException)
        {
            return Result<IReadOnlyList<T>>.Failure(Errors.FileAccess(path, ex.Message));
        }

        try
        {
            return Result<IReadOnlyList<T>>.Success(parse(lines));
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return Result<IReadOnlyList<T>>.Failure(Errors.MalformedContent(path, ex.Message));
        }
    }
}
