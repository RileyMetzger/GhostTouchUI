namespace GhostTouchUi.Model;

/// <summary>
/// Executes a callback repeatedly at randomized intervals within a configured range.
/// </summary>
public class RandomTimer : IDisposable
{
    private readonly TimerCallback _callback;
    private readonly object? _state;
    private readonly int _minTime;
    private readonly int _maxTime;
    private readonly Random _random;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task _timerTask = Task.CompletedTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomTimer"/> class and starts it immediately.
    /// </summary>
    /// <param name="callback">The callback to invoke after each delay interval.</param>
    /// <param name="state">A state object forwarded to each callback invocation.</param>
    /// <param name="minTime">The minimum delay between callbacks, in milliseconds.</param>
    /// <param name="maxTime">The maximum delay between callbacks, in milliseconds.</param>
    public RandomTimer(TimerCallback callback, object? state, int minTime, int maxTime)
    {
        if (minTime <= 0 || maxTime <= 0 || minTime > maxTime)
            throw new ArgumentException("Invalid minTime and maxTime values.");

        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _state = state;
        _minTime = minTime;
        _maxTime = maxTime;
        _random = new Random();
        _cancellationTokenSource = new CancellationTokenSource();

        StartTimer();
    }

    /// <summary>
    /// Starts the background task that waits for randomized intervals and invokes the callback.
    /// </summary>
    private void StartTimer()
    {
        _timerTask = Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                int delay = _random.Next(_minTime, _maxTime + 1);
                await Task.Delay(delay, _cancellationTokenSource.Token).ConfigureAwait(false);
                _callback(_state);
            }
        }, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Cancels the timer loop and releases its cancellation resources.
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        try
        {
            _timerTask.Wait();
        }
        catch (AggregateException)
        {
            // Ignore exceptions caused by cancellation
        }
        finally
        {
            _cancellationTokenSource.Dispose();
        }
    }
}
