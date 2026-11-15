

# Individual Contribution Report - BudgetPipeline



## Student and project information



- **Student:** Narges Jowkar

- **Group:** IT-2513

- **Course:** Introduction to Functional Programming

- **Project:** BudgetPipeline - Final Project

- **Team members:** Zhanbolat Tulepbergen, Narges Jowkar, and Almadi Askhat



## Project goal



BudgetPipeline is a C# and .NET 8 application that processes transaction and expense-request data from CSV files. It validates requests against category budgets, manages expense approval states, and generates financial reports. The project also integrates a small native C library for budget utilization calculations.



The application uses functional programming concepts such as explicit error handling, immutable state updates, pattern matching, functional composition, extension methods, and LINQ.



## My main contribution



My assigned technical area focused on functional error handling, optional-value handling, expense state management, transaction extension methods, LINQ reporting, and related automated tests.



|Source files | Main functionality |
| --- | --- |
| `Result.cs` | Represents successful and failed operations explicitly, supporting functional error handling and composition. |
| `Maybe.cs` | Represents optional values and missing lookup results without relying on null references. |
| `Error.cs` | Defines structured errors for validation and workflow failures. |
| `ExpenseModels.cs` | Defines the domain models and expense states used by the workflow. |
| `ExpenseWorkflow.cs` | Implements expense state transitions and validates operations against workflow and budget rules. |
| `TransactionExtensions.cs` | Provides reusable transaction transformations and category calculations. |
| `Reports.cs` | Generates category-level reports using LINQ filtering, grouping, aggregation, and ordering. |
| `Assignment7Tests.cs`, `Assignment8Tests.cs` | Contain tests covering functional errors, lookups, state transitions, and reporting behavior. |



## How my work connects with the rest of the project



The application's input layer reads CSV files and converts their contents into typed C# records. These records are then processed by the domain and reporting components.



`Result<T>` provides a consistent representation of successful and failed operations. Instead of using exceptions for expected validation failures, functions can return an explicit failure containing an error.



`Maybe<T>` represents optional values. It makes missing lookup results explicit and allows the caller to handle both existing and absent values.



The expense workflow uses the domain models, ledger, and budget information to validate expense operations. State transitions distinguish permitted operations from invalid ones.



An invalid transition produces a failure instead of silently changing the state. The workflow also needs to preserve the previous ledger when an operation cannot be completed successfully.



`TransactionExtensions` provides reusable operations for processing transaction collections. These transformations can be used without mixing transaction calculations with CSV input or console output.



`Reports` transforms transaction data into category-level summaries. LINQ expressions make the reporting logic more declarative by separating filtering, grouping, aggregation, and ordering.



Together, these modules connect validated input data with expense processing and financial reporting.



## Functional and programming concepts used



- **Explicit failures:** `Result<T>` represents success and failure as values.

- **Optional values:** `Maybe<T>` distinguishes existing values from missing values.

- **Functional composition:** `Map` and `Bind` allow operations to be combined while propagating failures.

- **Pattern matching:** Different result and expense-state cases can be handled explicitly.

- **Immutable state:** Successful workflow operations can return updated state without modifying the previous state.

- **Extension methods:** Reusable transformations improve readability and reduce duplicated code.

- **LINQ:** Filtering, grouping, projection, aggregation, and ordering are expressed declaratively.

- **Separation of concerns:** Error handling, workflow rules, and reporting remain separate from file input and output.

- **Deterministic output:** Consistent ordering rules help reports produce predictable results.



## Testing and verification



The group project report assigns 16 checks to this technical area, covering error handling, optional-value lookups, expense state transitions, and report calculations.



Relevant test scenarios include:



| Test area | Input or scenario | Expected behavior |
| --- | --- | --- |
| Result success | A successful operation | The resulting value remains accessible. |
| Result failure | An operation containing an error | The failure is preserved and propagated. |
| Maybe lookup | A value exists | The value is represented as present. |
| Maybe lookup | A value does not exist | The absence is represented explicitly. |
| Expense workflow | A valid state transition | The operation returns the appropriate updated state. |
| Expense workflow | An invalid state transition | The operation returns a failure. |
| Ledger preservation | An expense operation fails | The previous ledger remains unchanged. |
| Transaction processing | Transactions from different categories | Category calculations use the appropriate transactions. |
| Reporting | Transactions with different amounts and statuses | The report follows the defined filtering and aggregation rules. |
| Reporting edge cases | Zero-value and pending transactions | The report applies the intended inclusion and exclusion rules. |



The shared project report records a complete test run of **50 passed and 0 failed on 13 November 2026**.



The updated Release solution was recorded as building on **14 November 2026 with 0 warnings and 0 errors**.



The complete test suite still needs to be rerun after the latest scoring API and Linux native-support changes. These are recorded team results rather than a new personal test execution.



## Difficulties and limitations



A significant challenge in functional error handling is ensuring that failed operations do not continue through a processing pipeline as if they were successful.



Explicit result types and functional composition address this problem by making failure propagation part of the operation's return value.



Expense state management requires validation of permitted transitions. An expense should not be approved or processed through an invalid sequence, and failed operations should not change the existing ledger.



Category reporting introduces additional edge cases. The shared project report documents a defect involving zero-value transactions being counted and some pending transactions being omitted. The integrated reporting query was corrected to address these cases.



LINQ aggregation also requires careful filtering and ordering so that totals are accurate and output is predictable.



The current implementation uses application-defined budget limits and processes transaction data without a persistent database.



## Individual and shared work



My assigned technical responsibility covers the functional error-handling types, optional-value handling, expense state transitions, transaction extension methods, LINQ reporting, and the corresponding test areas.



CSV parsing, file loading, report export, application startup, and sample data belong to another assigned area.



Native C integration, scoring-related functionality, lazy processing, and other integration components belong to the remaining project areas or shared integration work.



The modules described in this report define the technical area I am responsible for explaining and demonstrating during the individual project defense.



## Repository reference



- **Repository:** https://github.com/zhanboty08-afk/final-IFP

- **Branch:** `main`

- **Reviewed source snapshot SHA:** `359c50940a201052f9e004726b55c45b3c14127e`

- **Final submission commit SHA:** Pending final team commit.



