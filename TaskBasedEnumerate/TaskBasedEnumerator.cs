using System.Collections;
using System.Threading.Tasks.Sources;

namespace TaskBasedEnumerate;

public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T>)
    {
        public static IEnumerable<T> Produce(Func<IEnumerationContext<T>, Task> block)
        {
            return new TaskBasedEnumerable<T>(block);
        }
    }
}

file sealed class TaskBasedEnumerable<T>(Func<IEnumerationContext<T>, Task> block) : IEnumerable<T>
{
    public IEnumerator<T> GetEnumerator() => new TaskBasedEnumerator<T>(block);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public interface IEnumerationContext<in T>
{
    public ValueTask Yield(T value); // Uses ValueTask because it should be awaited immediately.
}

file sealed class TaskBasedEnumerator<T>(Func<IEnumerationContext<T>, Task> block)
    : IEnumerator<T>, IEnumerationContext<T>, IValueTaskSource
{
    private Action<object?>? _continuation;
    private object? _state;

    private bool _initialized;
    private short _token;

    private T _current = default!;

    public bool MoveNext()
    {
        // Capture the current continuation + state.
        var continuation = _continuation;
        var state = _state;

        // Reset for the next iteration.
        _continuation = null;
        _state = null;

        switch (_initialized)
        {
            case true when continuation is not null:
                continuation(state);
                _token++;
                break;
            case false:
                _initialized = true;
                block(this);
                break;
            default:
                // Initialized, but no continuation.
                return false;
        }

        // If the block or the previous continuation set a new continuation, we have a next value
        return _continuation is not null;
    }

    public void Reset()
    {
        _continuation = null;
        _state = null;
        _initialized = false;
        _token++;
    }

    public ValueTask Yield(T value)
    {
        _current = value;
        return new ValueTask(this, _token);
    }

    public T Current => _initialized ? _current : throw new InvalidOperationException();


    object? IEnumerator.Current => Current;

    public void Dispose()
    {
    }

    public void GetResult(short token)
    {
        // Void - we have no result
        // Maybe throw if the token doesn't match?
    }

    public ValueTaskSourceStatus GetStatus(short token) =>
        _token == token
            ? ValueTaskSourceStatus.Pending
            : ValueTaskSourceStatus.Succeeded;

    public void OnCompleted(
        Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags
    )
    {
        if (_token != token)
        {
            // Maybe some throw?
            return;
        }

        _continuation = continuation;
        _state = state;
    }
}