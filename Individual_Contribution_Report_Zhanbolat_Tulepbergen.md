# Individual Contribution Report - BudgetPipeline

> **Draft:** Confirm the contribution and test details, then revise this report in your own words before submission.

## Student and project information

- **Student:** Zhanbolat Tulepbergen
- **Group:** IT-2513
- **Project:** BudgetPipeline — Final Project
- **Team members:** Zhanbolat Tulepbergen, Narges Jowkar, and Almadi Askhat

## Problem and goal

BudgetPipeline processes transaction and expense-request CSV files. It compares spending under different status policies, checks requests against category limits, applies safe approval rules, and produces reports. The goal is to make budget decisions clear and reproducible while keeping domain calculations separate from file and console operations.

## What I implemented

My contribution, as recorded in the shared README, focused on the budget pipeline, expense validation rules, Result-based expense operations, curried scoring, and lazy transaction processing.

| Source file | Types and methods | Contribution |
|---|---|---|
| `BudgetWorkflow.cs` | `SelectTransactions`, `CalculateCategoryTotals`, `CreateSummary`, `Run`, `IncludeApprovedOnly`, `IncludeApprovedAndPending` | Composed the three-stage summary pipeline and implemented two selectable status policies. |
| `ExpenseRules.cs`, `ExpenseModels.cs` | `RequireRequest`, `CheckId`, `CheckAmount`, `FindRoom`, `CheckBudget`, `ValidatedExpense.CreateResult` | Composed request checks so invalid data cannot become a validated expense. |
| `ExpenseOperations.cs` | `Submit`, `Approve`, `Process` | Connected validation and state transitions with `Result<T>.Map` and `Bind`; prevented failed operations from updating the ledger. |
| `Scoring.cs` | `TransactionScoring.CreateScore` | Built a genuinely curried scoring function, applied first to amount weight and then pending bonus. |
| `TransactionStream.cs` | `TransactionStream.Approved` | Implemented deferred filtering with `yield return`. |

## How my work integrates

`DataLoader` reads the transaction CSV and returns domain records. `Program.cs` loads inputs and `BudgetPipelineConsole.cs` sends them to `BudgetWorkflow`, which selects transactions according to a policy and calculates totals. Expense requests are checked by `ExpenseRules`; successful validation creates a pending state, and `ExpenseOperations` uses the current ledger when approving. The reports use the same transaction models, and the lazy stream exposes approved records without materialising them until the caller enumerates it.

The ledger is immutable from the workflow's point of view. An approval creates a new ledger. If a validation or approval fails, `Result<T>` carries the failure and the prior ledger remains available unchanged.

## Functional techniques and design reasons

- **Composition and named stages:** the pipeline separates selection, category aggregation, and summary construction, so each transformation can be checked independently.
- **Higher-order functions:** policies are passed as `Func<Transaction, bool>`. The same input can be evaluated under approved-only and approved-plus-pending rules.
- **Validated construction:** `ValidatedExpense` has a private constructor. Its factory creates the value only after the rules succeed.
- **Pattern matching and explicit cases:** expense states and `Result<T>` cases make allowed transitions and failures visible.
- **Map and Bind:** `ExpenseOperations.Process` chains dependent checks. A failure short-circuits later operations.
- **Immutable state:** ledger updates return a copy, preventing failed operations from leaving partial changes.
- **Currying and partial application:** `CreateScore` fixes the amount weight and returns a function that accepts the pending bonus, which returns the transaction scorer. The application shares the first partial application between two reports.
- **Lazy enumeration:** `yield return` postpones filtering until enumeration. The console prints before, during, and after iteration, then materializes a reusable snapshot.

## Tests I contributed and observed outcomes

The team's assignment contribution record attributes 16 tests to my contribution. They cover pipeline policy differences and purity, validation rules, Result-chain execution and failure propagation, preservation of state, curried scoring, and deferred execution timing. The combined console runner does not label tests by author, so I do not claim all tests in the integrated suite as my individual work. The console demonstrates the combined paths, while the current suite tests their constituent components rather than asserting the full console workflow in one test.

| Check area | Expected result | Last recorded combined-suite result (13 November 2026) |
|---|---|---|
| Approved-only and approved-plus-pending policies | Different totals for the same sample transactions; input remains unchanged | Passed; 15.50 and 40.50 respectively |
| Validation and budget boundaries | Valid requests pass; invalid IDs, amounts, categories, and overruns fail explicitly | Passed |
| Result chain | A failure stops dependent operations and preserves the prior ledger | Passed |
| Scoring and lazy processing | Configurations produce different scores; deferred work starts during enumeration | Passed |
| Entire integrated suite | Every check passes | Last recorded run: 50 passed, 0 failed (13 November 2026) |

The 13 November test results predate the latest scoring API and Linux native-support changes; the suite has not been rerun after those edits. The updated solution was built on 14 November 2026 with zero warnings and zero errors, and the application was run successfully. These results describe the shared suite and not a separately tagged personal test run.

## Technical difficulty and defect

A key difficulty in the expense workflow is that a request can pass initial validation but become unaffordable after an earlier request is approved. `Approve` therefore checks the remaining budget again immediately before updating the ledger. If the check fails, the operation returns a rejected state and the original ledger. The tests exercise this case and state preservation.

Final integration exposed a category-report mismatch: zero-value entries were counted and pending transactions in categories with approved activity were omitted. The report query was corrected, and the related checks now pass. Default data paths were also made independent of the terminal's current directory. These were shared integration fixes; I do not claim them as work I completed alone.

## Individual work versus shared work

My individual contribution is the pipeline, validation composition, Result-based expense operations, curried scoring, lazy approved stream, and the test areas listed above. The team shared the integrated codebase and final verification. Parser and file-boundary work, error and state types, extensions and reports, CLI/data integration, and documentation have separate owners in `README.md`.

The native C utilization feature and its two integration tests were added during final-project completion. They are part of the current shared project but were not part of my earlier assignment contribution. I need to review and understand this code before the oral defense.

## Incomplete work and project status

The application includes the C source, a C# P/Invoke adapter, reproducible Windows and Linux build scripts, Windows x64 native runtime, a real native-call test, and a controlled invalid-limit test. Linux x64 is supported when its native library is built with `bash ./build-native.sh`. The final Git commit ID must still be added after the changes are committed and pushed; no `.git` directory was available in the working copy.

