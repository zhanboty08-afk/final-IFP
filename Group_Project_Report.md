# BudgetPipeline — Group Project Report

## 1. Project information

- **Course:** Introduction to Functional Programming
- **Project:** BudgetPipeline
- **Team:** Zhanbolat Tulepbergen, Narges Jowkar, Almadi Askhat
- **Group:** IT-2513
- **Language/platform:** C# on .NET 8 with native C libraries for Windows x64 and Linux x64
- **Problem:** Transaction spending and expense requests need to be checked against category budgets with clear, reproducible results.
- **Intended users:** Individuals or small teams reviewing spending and budget requests.
- **Goals:** Compare transaction policies, validate and approve requests safely, explain failures, and produce useful reports using functional techniques.

## 2. Implemented scope and examples

The application accepts `transactions.csv` (`Id,Category,Amount,Status`) and `expense_requests.csv` (`Id,Category,Amount`). It demonstrates five end-to-end processing paths: policy-based summaries, state-based expense approval, Result-based expense processing, category reporting/export, and scored transaction reporting. Supporting integrations include:

1. A staged budget summary with approved-only and approved-plus-pending policies.
2. Expense request validation and sequential approval, which rechecks the current budget before creating a new ledger.
3. A Result-based processing pipeline in which failures stop dependent operations and preserve the prior ledger.
4. A category spending report and CSV export.
5. A scored transaction report using a curried scoring function.

The native C utilization report and deferred approved-transaction stream are additional demonstrations, not counted as separate end-to-end paths. The console demonstrates the complete paths; the dependency-free test runner focuses on domain, workflow, report, file-boundary, and native components rather than asserting the complete console output in one test.

For the included transaction sample, the approved-only policy selects 3 transactions and totals **15.50**. The approved-plus-pending policy selects 5 and totals **40.50**. The category report writes Food, 2 transactions, **17.50**, and Transport, 1 transaction, **3.00**. The native report returns Food **62.50%**, Transport **30.00%**, and Books **0.00%**; Gifts has a zero limit and is reported as a controlled native calculation failure. In the request sample, Food request 101 for 5.00 is approved; a Gym request is rejected as an unknown category; an over-budget Transport request is rejected.

## 3. Architecture and data flow

The related domain types include `Transaction`, `ExpenseRequest`, `ValidatedExpense`, `ExpenseState`, `BudgetLimits`, `BudgetSummary`, and `Ledger`. The data flow is:

```text
CSV -> DataLoader/parsers -> domain records -> workflows and reports
    -> NativeBudgetAdapter when utilization is needed -> console/CSV output
```

`Program.cs` loads input and `BudgetPipelineConsole.cs` coordinates console demonstrations; file access lives in `DataLoader.cs`. Domain transformations use immutable values and return new summaries or ledger states. The C interop boundary is isolated in `NativeBudgetAdapter.cs`.

## 4. Functional implementation

- `BudgetWorkflow.cs` composes `SelectTransactions`, `CalculateCategoryTotals`, and `CreateSummary`. It accepts a policy delegate; the two policies produce different summaries on the same data.
- `ValidatedExpense.CreateResult` composes `ExpenseRules` with `Bind` and `Map`. The private constructor prevents unchecked validated values.
- `ExpenseState` has explicit pending, approved, and rejected cases. `ExpenseWorkflow` and `ExpenseOperations` pattern-match these cases and reject invalid sequences without partially changing the ledger.
- `Result<T>` and `Error` model explicit domain, file, and native failures; `Maybe<T>` models absent lookups.
- `TransactionExtensions.cs` provides `WithStatus` and `ToCategoryTotals`. `Reports.cs` uses LINQ filtering, projection, `GroupBy`, `Count`, `Sum`, and deterministic ordering with tie-breakers.
- `TransactionScoring.CreateScore` is genuinely curried: it accepts the amount weight, then the pending bonus, then a transaction. The application reuses the first partial application for both reports.
- `TransactionStream.Approved` uses `yield return`. The application materializes a reusable snapshot with `ToArray` at its deliberate reuse point.

## 5. Persistence, integration, and resource handling

The CSV parsers validate headers, field counts, number formats, and status values. `DataLoader` owns a `StreamReader` in a `using` scope while reading, so the handle closes before parsing and also closes if reading fails. It maps missing files, access errors, and malformed records to explicit `Result` failures. Tests verify that files can be replaced after successful and failed reads. The exported category CSV is written with invariant decimal formatting. Failed approval operations leave the immutable previous ledger unchanged.

`BudgetPipeline/native/budget_native.c` exports `calculate_utilization_percent`. `NativeBudgetAdapter.cs` calls it through Cdecl P/Invoke and translates native status codes, missing libraries, entry points, or incompatible binaries into `NATIVE_INTEGRATION_ERROR`. The module stays loaded for the process lifetime; the application allocates no native buffers. Build on Windows with `build-native.ps1` and Visual Studio 2022 x64 C++ tools, or on Linux with `bash ./build-native.sh` and a C compiler. Runtime assets use `win-x64` and `linux-x64` folders.

## 6. Testing and defects

The dependency-free test runner includes 50 checks. Expected and observed outcomes include:

| Scenario | Expected | Observed |
|---|---|---|
| Pure pipeline and policies | Correct policy-dependent totals, input unchanged | Passed |
| Expense validation and approval | Invalid requests fail; current budget is rechecked; failed updates preserve the ledger | Passed |
| Result/Maybe workflows | Errors are explicit; `Map`/`Bind` stop dependent work; missing lookups are represented | Passed |
| Reports and curried scoring | Correct grouping, totals, filters, and stable tie order | Passed |
| File failures | Missing and malformed files return clear errors; file handles are released | Passed |
| Native C success | 15 spent / 20 limit returns 75% | Passed |
| Native controlled failure | Zero limit returns `NATIVE_INTEGRATION_ERROR` without an unhandled exception | Passed |
| Full suite | 50 passed, 0 failed | Last recorded run: **50 passed, 0 failed** (13 November 2026) |

The latest recorded test run predates the scoring API and Linux native-support changes; the suite has not been rerun after those edits. The updated Release solution built on 14 November 2026 with **0 warnings, 0 errors**, and the application ran successfully. During integration, category report tests exposed that zero-value transactions were counted and pending transactions in categories with approved activity were omitted. The query was corrected. Default input paths were also changed to be independent of the terminal's working directory.

## 7. Team responsibilities

| Member | Main code ownership | Tests/documentation |
|---|---|---|
| **Zhanbolat Tulepbergen** — IT-2513 | Budget summary pipeline, expense validation composition, Result-based operations, curried scoring, lazy stream | 16 tests across pipeline, policy, validation, state-preservation, scoring, and deferred-execution cases; architecture/integration documentation |
| **Narges Jowkar** — IT-2513 | Result/Maybe/error types, expense state transitions, extension methods, LINQ reports | 16 tests across error/lookup/state/report cases; functional error and report documentation |
| **Almadi Askhat** — IT-2513 | CSV parsers and boundary loading/export, ledger/limits, application CLI and sample data | 16 tests across parsing, file error, export, and resource cases; setup documentation |

The C library and final integration corrections are shared final-project additions. They are not attributed retroactively to an earlier assignment owner.

## 8. Setup and reproducibility

Prerequisites are Windows x64 or Linux x64 and the .NET 8 SDK. Windows also needs Visual Studio 2022 with the Desktop development with C++ workload; Linux needs a C compiler. From the solution directory, build the native library with the platform-specific script, then run:

```powershell
.\build-native.ps1
dotnet build BudgetPipeline.sln -c Release
dotnet run -c Release --project BudgetPipeline.Tests
dotnet run -c Release --project BudgetPipeline
```

On Linux, replace the first command with `bash ./build-native.sh`. The Windows DLL is included under `BudgetPipeline/runtimes/win-x64/native`; Linux creates `libbudget_native.so` under `BudgetPipeline/runtimes/linux-x64/native`. Existing runtime files are copied to application and test output. README.md contains custom file-path and malformed-input commands.

## 9. Limitations and references

- The CSV parser is intentionally simple and does not support quoted values containing commas.
- Budget limits are configured in `BudgetPipelineConsole.cs`; users cannot edit them through a menu or external configuration.
- Native integration targets Windows x64 and Linux x64; other operating systems are unsupported.
- The category report is exported, but updated transaction data is not persisted.
- The source uses the .NET Base Class Library and the platform C compiler (MSVC on Windows, GCC or Clang on Linux); no third-party runtime packages are used.
- References: `README.md`, the source files cited above, and the supplied Final Project Requirements document.
- All contributors are responsible for understanding the code they defend.

## 10. Repository and submitted version

- **Repository URL:** http://github.com/zhanboty08-afk/final-IFP
- **Branch:** `main`
- **Final commit identifier:** record the exact SHA after final changes are committed and pushed. The workspace provided for this work has no `.git` directory, so the SHA cannot be verified here.

Commit and push this version, confirm instructor access, replace the commit note with the exact SHA, and ensure the LMS archive matches that commit. `README.md` is the reproducible user/developer guide; each student's archive also needs that student's own individual contribution report.
