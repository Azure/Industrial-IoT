// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal readonly record struct ManagedItemRetryTarget(
        string Name,
        uint ClientHandle,
        long Generation,
        ManagedItemRetryKind Kind,
        StatusCode Status,
        bool Pending,
        bool Applied);

    internal readonly record struct ManagedRetryRequest(
        string? Name,
        uint ClientHandle,
        long Generation,
        ManagedItemRetryKind Kind,
        StatusCode Status,
        int Attempt);

    internal enum ManagedRetryOutcome
    {
        Started,
        Obsolete,
        Failed
    }

    internal sealed class ManagedSubscriptionRetryScheduler : IAsyncDisposable
    {
        public ManagedSubscriptionRetryScheduler(
            OpcUaSubscriptionOptions options,
            TimeProvider timeProvider,
            Func<ManagedRetryRequest, CancellationToken,
                ValueTask<ManagedRetryOutcome>> retry,
            ILogger? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeProvider = timeProvider ??
                throw new ArgumentNullException(nameof(timeProvider));
            _retry = retry ?? throw new ArgumentNullException(nameof(retry));
            _logger = logger ?? NullLogger.Instance;
            _timer = _timeProvider.CreateTimer(OnTimer, null,
                Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _states.Count;
                }
            }
        }

        public bool IsRetrying(string name)
        {
            lock (_lock)
            {
                return _states.TryGetValue(name, out var state) &&
                    state.Delay != TimeSpan.MaxValue;
            }
        }

        public Exception? LastError => Volatile.Read(ref _lastError);

        public void Update(ManagedItemRetryTarget target)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }
            lock (_lock)
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return;
                }
                if (target.Applied)
                {
                    _states.Remove(target.Name);
                    UpdateTimer();
                    return;
                }
                if (target.Pending)
                {
                    if (_states.TryGetValue(target.Name, out var pending) &&
                        pending.Generation == target.Generation)
                    {
                        pending.Phase = RetryPhase.Waiting;
                        pending.Version++;
                    }
                    else
                    {
                        _states.Remove(target.Name);
                    }
                    UpdateTimer();
                    return;
                }

                var kind = target.Kind;
                if (kind == ManagedItemRetryKind.None)
                {
                    _states.Remove(target.Name);
                    UpdateTimer();
                    return;
                }
                if (_states.TryGetValue(target.Name, out var current) &&
                    current.Generation == target.Generation &&
                    current.Kind == kind &&
                    current.Status == target.Status)
                {
                    if (current.Phase is RetryPhase.Processing or RetryPhase.Waiting)
                    {
                        IncrementAttempt(current);
                        Schedule(current);
                    }
                    UpdateTimer();
                    return;
                }

                var state = new RetryState(target.Name, target.ClientHandle,
                    target.Generation, kind, target.Status);
                _states[target.Name] = state;
                Schedule(state);
                UpdateTimer();
            }
        }

        public void UpdateSubscription(bool failed)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }
            lock (_lock)
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return;
                }
                if (!failed)
                {
                    _states.Remove(kSubscriptionKey);
                    UpdateTimer();
                    return;
                }
                if (_states.TryGetValue(kSubscriptionKey, out var current))
                {
                    if (current.Phase is RetryPhase.Processing or RetryPhase.Waiting)
                    {
                        IncrementAttempt(current);
                        Schedule(current);
                    }
                    UpdateTimer();
                    return;
                }

                var state = new RetryState(null, 0, 0,
                    ManagedItemRetryKind.Subscription,
                    StatusCodes.BadSubscriptionIdInvalid);
                _states[kSubscriptionKey] = state;
                Schedule(state);
                UpdateTimer();
            }
        }

        public void Remove(string name)
        {
            lock (_lock)
            {
                _states.Remove(name);
                UpdateTimer();
            }
        }

        public ValueTask ProcessAsync(bool force = false,
            CancellationToken ct = default)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return ValueTask.CompletedTask;
            }
            return new ValueTask(ProcessCoreAsync(force, ct));
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            await _cts.CancelAsync().ConfigureAwait(false);
            Task? worker;
            lock (_taskLock)
            {
                worker = _worker;
            }
            if (worker != null)
            {
                await worker.ConfigureAwait(false);
            }
            lock (_lock)
            {
                _states.Clear();
            }
            _timer.Dispose();
            _runGate.Dispose();
            _cts.Dispose();
        }

        private void Schedule(RetryState state)
        {
            state.Delay = ManagedSubscriptionRetryPolicy.GetDelay(
                state.Kind, _options, state.Attempt);
            state.StartedAt = _timeProvider.GetTimestamp();
            state.Phase = RetryPhase.Scheduled;
            state.Version++;
        }

        private static void IncrementAttempt(RetryState state)
        {
            if (state.Attempt < int.MaxValue)
            {
                state.Attempt++;
            }
        }

        private void UpdateTimer()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }
            var now = _timeProvider.GetTimestamp();
            var due = _states.Values
                .Where(state => state.Phase == RetryPhase.Scheduled &&
                    state.Delay != TimeSpan.MaxValue)
                .Select(state => GetRemaining(state, now))
                .DefaultIfEmpty(Timeout.InfiniteTimeSpan)
                .Min();
            _timer.Change(due, Timeout.InfiniteTimeSpan);
        }

        private TimeSpan GetRemaining(RetryState state, long now)
        {
            var elapsed = _timeProvider.GetElapsedTime(state.StartedAt, now);
            return elapsed >= state.Delay
                ? TimeSpan.Zero
                : state.Delay - elapsed;
        }

        private void OnTimer(object? state)
        {
            lock (_taskLock)
            {
                if (_worker == null && Volatile.Read(ref _disposed) == 0)
                {
                    _worker = RunTimerAsync();
                }
            }
        }

        private async Task RunTimerAsync()
        {
            await Task.Yield();
            try
            {
                await ProcessCoreAsync(false, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Volatile.Write(ref _lastError, ex);
                _logger.ManagedRetryProcessingFailed(ex);
            }
            finally
            {
                lock (_taskLock)
                {
                    _worker = null;
                }
                lock (_lock)
                {
                    UpdateTimer();
                }
            }
        }

        private async Task ProcessCoreAsync(bool force, CancellationToken ct)
        {
            await _runGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                List<(ManagedRetryRequest Request, long Version)> due;
                lock (_lock)
                {
                    var now = _timeProvider.GetTimestamp();
                    due = _states.Values
                        .Where(state => state.Phase == RetryPhase.Scheduled &&
                            state.Delay != TimeSpan.MaxValue &&
                            (force || GetRemaining(state, now) == TimeSpan.Zero))
                        .Select(state =>
                        {
                            state.Phase = RetryPhase.Processing;
                            var version = ++state.Version;
                            return (state.ToRequest(), version);
                        })
                        .ToList();
                    UpdateTimer();
                }

                foreach (var (request, version) in due)
                {
                    var key = request.Name ?? kSubscriptionKey;
                    lock (_lock)
                    {
                        if (!_states.TryGetValue(key, out var current) ||
                            current.Version != version)
                        {
                            continue;
                        }
                    }
                    ManagedRetryOutcome outcome;
                    try
                    {
                        outcome = await _retry(request, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Volatile.Write(ref _lastError, ex);
                        _logger.ManagedRetryAttemptFailed(ex,
                            request.Name ?? kSubscriptionKey, request.Attempt);
                        outcome = ManagedRetryOutcome.Failed;
                    }
                    lock (_lock)
                    {
                        if (!_states.TryGetValue(key, out var state) ||
                            state.Version != version)
                        {
                            continue;
                        }
                        switch (outcome)
                        {
                            case ManagedRetryOutcome.Started:
                                state.Phase = RetryPhase.Waiting;
                                state.Version++;
                                break;
                            case ManagedRetryOutcome.Obsolete:
                                _states.Remove(key);
                                break;
                            default:
                                IncrementAttempt(state);
                                Schedule(state);
                                break;
                        }
                    }
                }
                lock (_lock)
                {
                    UpdateTimer();
                }
            }
            finally
            {
                lock (_lock)
                {
                    foreach (var state in _states.Values
                        .Where(state => state.Phase == RetryPhase.Processing))
                    {
                        IncrementAttempt(state);
                        Schedule(state);
                    }
                    UpdateTimer();
                }
                _runGate.Release();
            }
        }

        private enum RetryPhase
        {
            Scheduled,
            Processing,
            Waiting
        }

        private sealed class RetryState
        {
            public string? Name { get; }
            public uint ClientHandle { get; }
            public long Generation { get; }
            public ManagedItemRetryKind Kind { get; }
            public StatusCode Status { get; }
            public int Attempt { get; set; } = 1;
            public long StartedAt { get; set; }
            public TimeSpan Delay { get; set; }
            public RetryPhase Phase { get; set; }
            public long Version { get; set; }

            public RetryState(string? name, uint clientHandle,
                long generation, ManagedItemRetryKind kind,
                StatusCode status)
            {
                Name = name;
                ClientHandle = clientHandle;
                Generation = generation;
                Kind = kind;
                Status = status;
            }

            public ManagedRetryRequest ToRequest()
            {
                return new ManagedRetryRequest(Name, ClientHandle,
                    Generation, Kind, Status, Attempt);
            }
        }

        private readonly OpcUaSubscriptionOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly Func<ManagedRetryRequest, CancellationToken,
            ValueTask<ManagedRetryOutcome>> _retry;
        private readonly ILogger _logger;
        private readonly Lock _lock = new();
        private readonly Dictionary<string, RetryState> _states =
            new(StringComparer.Ordinal);
        private readonly Lock _taskLock = new();
        private readonly ITimer _timer;
        private readonly SemaphoreSlim _runGate = new(1, 1);
        private readonly CancellationTokenSource _cts = new();
        private Task? _worker;
        private Exception? _lastError;
        private int _disposed;
        private const string kSubscriptionKey = "\0subscription";
    }

    internal static partial class ManagedSubscriptionRetrySchedulerLogging
    {
        [LoggerMessage(EventId = 1131, Level = LogLevel.Error,
            Message = "Managed retry processing failed.")]
        public static partial void ManagedRetryProcessingFailed(
            this ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1132, Level = LogLevel.Warning,
            Message = "Managed retry attempt {Attempt} for {Item} failed.")]
        public static partial void ManagedRetryAttemptFailed(
            this ILogger logger, Exception exception, string item, int attempt);
    }
}
