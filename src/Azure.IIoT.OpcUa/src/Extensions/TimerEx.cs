// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa
{
    using System.Threading;
    using System.Diagnostics;
    using System;

    /// <summary>
    /// Timer expired event argument
    /// </summary>
    public class ElapsedEventArgs : EventArgs
    {
        /// <summary>
        /// Signal time
        /// </summary>
        public DateTimeOffset SignalTime { get; }

        internal ElapsedEventArgs(DateTimeOffset localTime)
        {
            SignalTime = localTime;
        }
    }

    /// <summary>
    /// Handles recurring events in an application. This is a simplfied
    /// version of the System.Timers.Timer class but uses a TimeProvider
    /// provided ITimer instead of the default System.Threading.Timer.
    /// </summary>
    public sealed class TimerEx : IDisposable
    {
        /// <summary>
        /// Gets or sets a value indicating whether the Timer raises
        /// the Tick event each time the specified Interval has elapsed,
        /// when Enabled is set to true.
        /// </summary>
        public bool AutoReset
        {
            get => _autoReset;
            set
            {
                if (_autoReset != value)
                {
                    _autoReset = value;
                    if (_timer != null)
                    {
                        UpdateTimer();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Timer
        /// is able to raise events at a defined interval.
        /// The default value by design is false, don't change it.
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    if (!value)
                    {
                        if (_timer != null)
                        {
                            _cookie = null;
                            _timer.Dispose();
                            _timer = null;
                        }
                        _enabled = value;
                    }
                    else
                    {
                        _enabled = value;
                        if (_timer == null)
                        {
                            ObjectDisposedException.ThrowIf(_disposed, this);

                            _cookie = new object();
                            _timer = _timeProvider.CreateTimer(_callback,
                                _cookie, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                            _timer.Change(_interval,
                                _autoReset ? _interval : Timeout.InfiniteTimeSpan);
                        }
                        else
                        {
                            UpdateTimer();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create new timer
        /// </summary>
        /// <param name="timeProvider"></param>
        public TimerEx(TimeProvider? timeProvider = null)
        {
            _interval = Timeout.InfiniteTimeSpan;
            _enabled = false;
            _autoReset = true;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _callback = new TimerCallback(MyTimerCallback);
        }

        /// <summary>
        /// Create timer with interval
        /// </summary>
        /// <param name="interval">
        /// The time between events. The value in milliseconds must be greater
        /// than zero and less than or equal to <see cref="int.MaxValue"/>.
        /// </param>
        /// <param name="timeProvider"></param>
        public TimerEx(TimeSpan interval, TimeProvider? timeProvider = null) :
            this(timeProvider)
        {
            _interval = interval;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Close();
            _disposed = true;
        }

        private void UpdateTimer()
        {
            Debug.Assert(_timer != null, $"{nameof(_timer)} was expected not to be null");
            _timer.Change(_interval, _autoReset ? _interval : Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Gets or sets the interval on which to raise events.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public TimeSpan Interval
        {
            get => _interval;
            set
            {
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentException("Bad interval");
                }

                _interval = value;
                if (_timer != null)
                {
                    UpdateTimer();
                }
            }
        }

        /// <summary>
        /// Occurs when the Interval> has elapsed.
        /// </summary>
        public event EventHandler<ElapsedEventArgs> Elapsed
        {
            add => _onIntervalElapsed += value;
            remove => _onIntervalElapsed -= value;
        }

        /// <summary>
        /// Closes the current timer object.
        /// </summary>
        public void Close()
        {
            _enabled = false;

            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        /// <summary>
        /// Starts the timing by setting Enabled property.
        /// </summary>
        public void Start()
        {
            Enabled = true;
        }

        /// <summary>
        /// Stops the timing by setting the Enabled property.
        /// </summary>
        public void Stop()
        {
            Enabled = false;
        }

        private void MyTimerCallback(object? state)
        {
            // System.Threading.Timer will not cancel the work item queued before the timer is stopped.
            // We don't want to handle the callback after a timer is stopped.
            if (state != _cookie)
            {
                return;
            }

            if (!_autoReset)
            {
                _enabled = false;
            }

            var elapsedEventArgs = new ElapsedEventArgs(_timeProvider.GetUtcNow());
            try
            {
                // To avoid race between remove handler and raising the event
                EventHandler<ElapsedEventArgs>? intervalElapsed = _onIntervalElapsed;
                if (intervalElapsed != null)
                {
                    intervalElapsed(this, elapsedEventArgs);
                }
            }
            catch
            {
                // Silently eat user exception
            }
        }

        private TimeSpan _interval;
        private bool _enabled;
        private readonly TimeProvider _timeProvider;
        private EventHandler<ElapsedEventArgs>? _onIntervalElapsed;
        private bool _autoReset;
        private bool _disposed;
        private ITimer? _timer;
        private readonly TimerCallback _callback;
        private object? _cookie;
    }
}
