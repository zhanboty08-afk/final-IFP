# Individual Contribution Report - BudgetPipeline

## Student and project information

- **Student:** Almadi Askhat
- **Group:** IT-2513
- **Course:** Introduction to Functional Programming
- **Project:** BudgetPipeline - Final Project
- **Team members:** Zhanbolat Tulepbergen, Narges Jowkar, and Almadi Askhat

## Project goal

BudgetPipeline reads transaction and expense-request data from CSV files. It checks transaction spending against category budgets, validates new expense requests, and creates reports. The project uses C# and .NET 8, with a small native C library for budget utilization calculations.

## My main contribution

My assigned part focused on the data input and output boundary and the connection between the CSV data and the application. It included CSV parsing, file loading and error handling, category report export, ledger and budget-limit support, application startup, and the sample data files.

| Source files | Main work |
|---|---|
| `TransactionParser.cs` | Parses transaction CSV rows and checks the header, number of fields, integer ID, category, decimal amount, and transaction status. It rejects negative transaction amounts and unknown statuses. |
| `ExpenseRequestParser.cs` | Parses expense-request rows and checks the CSV structure and numeric fields. |
| `DataLoader.cs` | Loads transactions and requests, creates the initial ledger from approved transactions, and converts file and parsing problems into `Result` failures. |
| `DataLoader.cs` | Exports the category report to CSV, creates the output directory when needed, and uses invariant formatting for decimal values. |
| `Ledger.cs`, `BudgetLimits.cs` | Provides category spending and limit lookups, calculates remaining budget, and creates a new ledger when an expense is added. |
| `Program.cs`, `BudgetPipelineConsole.cs` | Connects command-line paths, default sample files, input loading, and the console demonstrations. |
| `BudgetPipeline/data/` | Provides sample transaction, expense-request, malformed-input, and report CSV files for demonstration and testing. |

## How my work connects with the rest of the project

The application starts in `Program.cs`. It resolves the default sample paths from the application's output directory or accepts paths provided as command-line arguments. It then calls `DataLoader` to load the transaction and request data. The parsers convert CSV text into typed C# records, so the workflows do not need to parse raw strings themselves.

`DataLoader` keeps file access separate from the business logic. It reads the file inside a `using` scope so the file handle is released after reading, including when reading fails. Missing files, access problems, and malformed content are returned as explicit errors through `Result<T>` rather than being allowed to stop the application unexpectedly.

The initial ledger is created from approved transaction totals. `Ledger` provides the amount already spent in each category and can calculate the remaining amount using `BudgetLimits`. When a valid expense is approved, the workflow receives a new ledger value instead of changing the old one.

The report export converts category totals into CSV rows. Decimal values use invariant formatting, so the exported file uses a dot as the decimal separator regardless of the computer's regional settings.

## Functional and programming concepts used

- **Parsing at the boundary:** raw CSV strings are validated before they enter the main workflows.
- **Explicit failures:** file and parsing errors are returned through `Result<T>` with a useful error description.
- **Resource management:** `StreamReader` is used inside a `using` scope so the file is closed after loading.
- **Culture-independent formatting:** CSV report amounts are formatted with `CultureInfo.InvariantCulture`.
- **Immutable ledger updates:** adding an approved expense creates a new ledger and keeps the previous ledger available.
- **Command-line arguments:** the user can run the application with the sample data or provide custom CSV paths.

## Testing and verification

The group project report assigns 16 checks to my area, covering CSV parsing, file errors, report export, and resource handling. The shared test runner currently registers 50 checks in total. Its relevant cases include request parsing, missing and malformed files, valid file loading, report CSV export, and checking that a loaded file can be changed after the read operation completes. The runner also checks invalid headers, field values, negative transaction amounts, header-only input, and unsupported statuses.

The last recorded complete team run was **50 passed, 0 failed on 13 November 2026**. This run predates the latest scoring API and Linux native-support edits. The updated Release solution was recorded as building on **14 November 2026 with 0 warnings and 0 errors**, and the application ran successfully. The full test suite was not rerun after those edits, so a new run is required before final submission. I do not present the historical test result as a fresh run.

## Difficulties and limitations

A file can be missing, blocked by permissions, or contain invalid rows. The loader handles these cases separately and returns a clear failure instead of using incomplete data. Another important detail is closing the file after reading; otherwise, a file may remain locked and cannot be replaced by a test or another program.

The current CSV parser is intentionally simple and does not support quoted values containing commas. Budget limits are configured in the application code. The report can be exported, but transaction data is not stored in a database.

## Individual and shared work

My assigned contribution is the CSV parsers, file loading and export, ledger and budget-limit support, application CLI, and sample data described above. Expense state transitions, Result/Maybe types, extension methods, LINQ report calculations, scoring, lazy processing, and native C integration belong to other assigned areas or shared integration work. I do not claim those parts as individual work.

## Repository reference

- **Repository:** https://github.com/zhanboty08-afk/final-IFP
- **Branch:** `main`
- **Reviewed source snapshot SHA (before this cleanup package):** `359c50940a201052f9e004726b55c45b3c14127e`
- **Final submission commit SHA:** pending; it must be recorded after the final files are committed and pushed.
