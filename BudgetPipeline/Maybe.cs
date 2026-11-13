namespace BudgetPipeline;

public abstract class Maybe<T>
{
    private Maybe() { }

    public static Maybe<T> Some(T value) => new Present(value);

    public static Maybe<T> None() => Absent.Instance;

    public bool IsPresent => this is Present;

    public TOut Match<TOut>(Func<T, TOut> whenPresent, Func<TOut> whenAbsent) => this switch
    {
        Present present => whenPresent(present.Value),
        Absent => whenAbsent(),
        _ => throw new InvalidOperationException("Unknown maybe case.")
    };

    public sealed class Present : Maybe<T>
    {
        public T Value { get; }

        internal Present(T value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value), "Use None for a missing value.");
            Value = value;
        }
    }

    public sealed class Absent : Maybe<T>
    {
        internal static readonly Absent Instance = new();

        private Absent() { }
    }
}

public static class Maybe
{
    public static Maybe<T> Some<T>(T value) => Maybe<T>.Some(value);

    public static Maybe<T> None<T>() => Maybe<T>.None();

    public static Maybe<T> FromNullable<T>(T? value) where T : class =>
        value is null ? None<T>() : Some(value);
}

public static class MaybeExtensions
{
    public static Maybe<U> Map<T, U>(this Maybe<T> maybe, Func<T, U> map) =>
        maybe.Match(value => Maybe.Some(map(value)), () => Maybe.None<U>());

    public static Result<T> ToResult<T>(this Maybe<T> maybe, Error whenAbsent) =>
        maybe.Match(value => Result<T>.Success(value), () => Result<T>.Failure(whenAbsent));
}
