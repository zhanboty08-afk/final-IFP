# BudgetPipeline

**Course:** Introduction to Functional Programming  
**Team:** Zhanbolat Tulepbergen, Narges Jowkar, Almadi Askhat  
**Group:** IT-2513  
**Stack:** C# / .NET 8 and native C  
**Supported platforms:** Windows x64 and Linux x64

BudgetPipeline reads transaction and expense-request CSV files, checks requests against category budgets, and produces spending reports. It demonstrates a functional core with immutable state, explicit failures, configurable rules, LINQ reports, lazy processing, and a native C calculation.

## Project documents

- [Group project report](Group_Project_Report.md) — scope, architecture, implementation, tests, roles, limitations, and submission details.
- [Individual contribution report — Zhanbolat Tulepbergen](Individual_Contribution_Report_Zhanbolat_Tulepbergen.md)

## Main workflows

The application demonstrates five end-to-end paths: policy-based transaction summaries; expense validation and sequential approval; a separate short-circuiting `Result<T>` expense pipeline; category reporting with CSV export; and scored transaction reporting. Native budget utilisation and lazy approved-transaction enumeration are additional integration demonstrations.

The A6 state workflow returns all applicable validation reasons and explicit pending, approved, or rejected states. The A7 `Result<T>` workflow is intentionally separate: it returns the first structured error and composes dependent operations with `Map` and `Bind`.

The bundled transaction data produces totals of **15.50** for approved transactions and **40.50** for approved plus pending. The category report contains Food (**2 transactions, 17.50**) and Transport (**1 transaction, 3.00**). The native report shows Food at **62.50%**, Transport at **30.00%**, and Books at **0.00%** of their limits. Gifts has a zero limit and returns a handled validation failure.

## Source map

| Area | Files and entry points |
|---|---|
| Budget pipeline | `BudgetWorkflow.cs`: `SelectTransactions`, `CalculateCategoryTotals`, `CreateSummary`, `Run` |
| Expense validation and operations | `ExpenseRules.cs`, `ExpenseModels.cs`, `ExpenseWorkflow.cs`, `ExpenseOperations.cs` |
| Explicit results and lookups | `Result.cs`, `Maybe.cs`, `Error.cs` |
| Extensions and reports | `TransactionExtensions.cs`, `Reports.cs` |
| Curried scoring and lazy stream | `Scoring.cs`, `TransactionStream.cs` |
| CSV boundary and CLI | `DataLoader.cs`, `TransactionParser.cs`, `ExpenseRequestParser.cs`; `Program.cs` loads inputs, while `BudgetPipelineConsole.cs` coordinates the demos |
| Console demos | `ReportingDemo.cs`, `BudgetUtilizationDemo.cs`, `ExpenseWorkflowDemo.cs`, `ResultPipelineDemo.cs`, `LookupDemo.cs`, `FileFailureDemo.cs` |
| Native C integration | `native/budget_native.c`, `NativeBudgetAdapter.cs` |

Native build scripts: `build-native.ps1` for Windows x64 and `build-native.sh` for Linux x64.

## Requirements implemented

The solution includes pure staged transformations, policy delegates, immutable domain values, validated construction, explicit state cases, `Result`/`Maybe`, two custom extension methods, two LINQ reports, deterministic tie-breakers, a genuinely curried scoring function applied in stages, a deferred `yield return` stream, CSV loading/export, and native C integration. The test runner covers domain and workflow cases, file failures, reports, lazy processing, resource release, and native success/failure. See the [group report](Group_Project_Report.md) for requirements traceability and test outcomes.

## Build, run, and test

### Prerequisites

- Windows x64 or Linux x64
- .NET 8 SDK
- Windows: Visual Studio 2022 with the **Desktop development with C++** workload
- Linux: a C compiler such as `gcc` or `clang`

From PowerShell in the folder containing `BudgetPipeline.sln` (Windows):

```powershell
.\build-native.ps1
dotnet build BudgetPipeline.sln -c Release
dotnet run -c Release --project BudgetPipeline.Tests
dotnet run -c Release --project BudgetPipeline
```

On Linux, open a terminal in the solution folder and run `bash ./build-native.sh`, then `dotnet build BudgetPipeline.sln -c Release` and `dotnet run -c Release --project BudgetPipeline`. The scripts place the library under the platform-specific `BudgetPipeline/runtimes/<rid>/native` folder. The project copies it beside the application and tests. Bundled CSV paths are resolved from the application directory.

To supply custom paths:

```powershell
dotnet run -c Release --project BudgetPipeline -- BudgetPipeline/data/transactions.csv BudgetPipeline/data/expense_requests.csv BudgetPipeline/data/category_spending_report.csv
```

To demonstrate missing and malformed input:

```powershell
dotnet run -c Release --project BudgetPipeline -- BudgetPipeline/data/transactions.csv missing.csv
dotnet run -c Release --project BudgetPipeline -- BudgetPipeline/data/transactions.csv BudgetPipeline/data/expense_requests_malformed.csv
```

The expected test result is `Tests: 50 passed, 0 failed.`

## Limitations

- The CSV parser does not support quoted values containing commas.
- Budget limits are configured in `BudgetPipelineConsole.cs`.
- Native integration currently supports Windows x64 and Linux x64.
- The application exports the category report but does not persist updated transaction data.

## Repository and submitted version

- **Repository URL:** http://github.com/zhanboty08-afk/final-IFP 
- **Branch:** `main`

The submission archive should include the solution, C# and C sources, native DLL/build script, sample CSV data, tests, this README, `Group_Project_Report.md`, and the submitting student's individual report. Exclude `bin/`, `obj/`, `.native-build/`, IDE caches, and credentials.
