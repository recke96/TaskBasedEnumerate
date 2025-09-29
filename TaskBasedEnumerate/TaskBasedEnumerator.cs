using System.Collections;

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
    : IEnumerator<T>, IEnumerationContext<T>
{
    private static readonly object _sentinel = new();

    private SemaphoreSlim _read = new(0, 1);
    private SemaphoreSlim _write = new(0, 1);

    private Task? _blockTask;
    private object? _current = _sentinel;

    public bool MoveNext()
    {
        _write.Release(); // Run to the next yield.
        _blockTask ??= block(this); // Run the block if it is not already running.

        if (_blockTask.IsCompleted)
        {
            // When the block is completed, the enumerator is done.
            return false;
        }

        _read.Wait(); // Wait for yield
        return true;
    }

    public void Reset()
    {
        _blockTask = null;
        _current = _sentinel;
        _read.Dispose();
        _write.Dispose();
        _read = new SemaphoreSlim(0, 1);
        _write = new SemaphoreSlim(0, 1);
    }

    public T Current => ReferenceEquals(_sentinel, _current)
        ? throw new InvalidOperationException("Not initialized (Call MoveNext() first).")
        : _blockTask?.IsCompleted == true
            ? throw new InvalidOperationException("No such element.")
            : (T)_current!;

    object? IEnumerator.Current => Current;

    public void Dispose()
    {
        _read.Dispose();
        _write.Dispose();
    }

    public async ValueTask Yield(T value)
    {
        await _write.WaitAsync(); // Wait for MoveNext
        _current = value;
        _read.Release();
    }
}