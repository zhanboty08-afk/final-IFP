namespace BudgetPipeline;

public sealed record CategoryRoom(ExpenseRequest Request, string Category, decimal Remaining);

public static class ExpenseRules
{
    public static Result<ExpenseRequest> RequireRequest(ExpenseRequest? request) =>
        request is null
            ? Result<ExpenseRequest>.Failure(Errors.MissingRequest())
            : Result<ExpenseRequest>.Success(request);

    public static Result<ExpenseRequest> CheckId(ExpenseRequest request) =>
        request.Id > 0
            ? Result<ExpenseRequest>.Success(request)
            : Result<ExpenseRequest>.Failure(Errors.InvalidId(request.Id));

    public static Result<ExpenseRequest> CheckAmount(ExpenseRequest request) =>
        request.Amount > 0
            ? Result<ExpenseRequest>.Success(request)
            : Result<ExpenseRequest>.Failure(Errors.InvalidAmount(request.Id, request.Amount));

    public static Result<CategoryRoom> FindRoom(ExpenseRequest request, BudgetLimits limits, Ledger ledger)
    {
        var category = (request.Category ?? string.Empty).Trim();

        return limits.Find(category)
            .ToResult(Errors.UnknownCategory(category))
            .Map(limit => new CategoryRoom(request, category, limit - ledger.Spent(category)));
    }

    public static Result<CategoryRoom> CheckBudget(CategoryRoom room) =>
        room.Request.Amount <= room.Remaining
            ? Result<CategoryRoom>.Success(room)
            : Result<CategoryRoom>.Failure(
                Errors.BudgetExceeded(room.Category, room.Remaining, room.Request.Amount));
}
