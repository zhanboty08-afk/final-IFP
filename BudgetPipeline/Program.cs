using BudgetPipeline;

var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
var transactionsPath = args.Length > 0 ? args[0] : Path.Combine(dataDirectory, "transactions.csv");
var requestsPath = args.Length > 1 ? args[1] : Path.Combine(dataDirectory, "expense_requests.csv");
var reportOutputPath = args.Length > 2 ? args[2] : Path.Combine(dataDirectory, "category_spending_report.csv");

var loaded = DataLoader.LoadTransactions(transactionsPath)
    .Bind(transactions => DataLoader.LoadRequests(requestsPath)
        .Map(requests => (Transactions: transactions, Requests: requests)));

Environment.ExitCode = loaded.Match(
    data =>
    {
        BudgetPipelineConsole.Run(data.Transactions, data.Requests, transactionsPath, reportOutputPath);
        return 0;
    },
    error =>
    {
        Console.Error.WriteLine($"Input error: {error}");
        return 1;
    });
