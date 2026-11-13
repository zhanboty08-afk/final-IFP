namespace BudgetPipeline;

public abstract class Result<T>
{
    private Result() { }

    public static Result<T> Success(T value) => new Ok(value);

    public static Result<T> Failure(Error error) => new Fail(error);

    public bool IsSuccess => this is Ok;

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) => this switch
    {
        Ok ok => onSuccess(ok.Value),
        Fail fail => onFailure(fail.Error),
        _ => throw new InvalidOperationException("Unknown result case.")
    };

    public sealed class Ok : Result<T>
    {
        public T Value { get; }

        internal Ok(T value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value), "A successful result must carry a value.");
            Value = value;
        }
    }

    public sealed class Fail : Result<T>
    {
        public Error Error { get; }

        internal Fail(Error error)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error), "A failed result must carry an error.");
        }
    }
}

public static class ResultExtensions
{
    public static Result<U> Map<T, U>(this Result<T> result, Func<T, U> map) => result switch
    {
        Result<T>.Ok ok => Result<U>.Success(map(ok.Value)),
        Result<T>.Fail fail => Result<U>.Failure(fail.Error),
        _ => throw new InvalidOperationException("Unknown result case.")
    };

    public static Result<U> Bind<T, U>(this Result<T> result, Func<T, Result<U>> bind) => result switch
    {
        Result<T>.Ok ok => bind(ok.Value),
        Result<T>.Fail fail => Result<U>.Failure(fail.Error),
        _ => throw new InvalidOperationException("Unknown result case.")
    };
}
