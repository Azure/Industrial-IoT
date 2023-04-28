/* ========================================================================
 * Copyright (c) 2005-2017 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Test
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    public sealed class FastTimer : ITimer
    {
        /// <summary>
        /// Initializes a new instance of the FastTimer class and sets all properties to their default values
        /// </summary>
        public FastTimer()
        {
        }

        /// <summary>
        /// Initializes a new instance of the FastTimer class and sets all properties to their default values except interval
        /// </summary>
        /// <param name="interval"></param>
        public FastTimer(double interval)
        {
            Interval = interval;
        }

        /// <summary>
        /// Property that sets if the timer should restart when an event has been fired
        /// </summary>
        public bool AutoReset { get; set; } = true;

        /// <summary>
        /// Is this timer currently running?
        /// </summary>
        public bool Enabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                if (_isEnabled)
                {
                    Start();
                }
                else
                {
                    Stop();
                }
            }
        }

        /// <summary>
        /// The current interval between triggering of this timer
        /// </summary>
        public double Interval { get; set; }

        /// <summary>
        /// The event handler we call when the timer is triggered
        /// </summary>
        public event EventHandler<FastTimerElapsedEventArgs> Elapsed;

        public void Close()
        {
            Enabled = false;
        }

        /// <summary>
        /// Starts the timer
        /// </summary>
        private void Start()
        {
            var isRunning = Interlocked.Exchange(ref _isRunning, 1);
            if (isRunning == 0)
            {
                var thread = new Thread(Runner)
                {
                    Priority = ThreadPriority.Highest
                };
                thread.Start();
            }
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        private void Stop()
        {
            Interlocked.Exchange(ref _isRunning, 0);
        }

        private void Runner()
        {
            double nextTrigger = 0f;

            var sw = new Stopwatch();
            sw.Start();

            while (_isRunning == 1)
            {
                WaitInterval(sw, ref nextTrigger);
                if (_isRunning == 1)
                {
                    Elapsed?.Invoke(this, new FastTimerElapsedEventArgs());

                    if (!AutoReset)
                    {
                        Interlocked.Exchange(ref _isRunning, 0);
                        Enabled = false;
                        break;
                    }

                    // restarting the timer in every hour to prevent precision problems
                    if (sw.Elapsed.TotalHours >= 1d)
                    {
                        sw.Restart();
                        nextTrigger = 0f;
                    }
                }
            }

            sw.Stop();
        }

        private void WaitInterval(Stopwatch sw, ref double nextTrigger)
        {
            var intervalLocal = Interval;
            nextTrigger += intervalLocal;

            while (true)
            {
                var elapsed = sw.ElapsedTicks * kTickFrequency;
                var diff = nextTrigger - elapsed;
                if (diff <= 0f)
                {
                    break;
                }

                if (diff < 1f)
                {
                    Thread.SpinWait(10);
                }
                else if (diff < 10f)
                {
                    Thread.SpinWait(100);
                }
                else
                {
                    if (diff >= 16f)
                    {
                        Thread.Sleep(diff >= 100f ? 50 : 1);
                    }
                    else
                    {
                        Thread.SpinWait(1000);
                        Thread.Sleep(0);
                    }

                    // if we have a larger time to wait, we check if the interval has been changed in the meantime
                    var newInterval = Interval;

                    if (intervalLocal != newInterval)
                    {
                        nextTrigger += newInterval - intervalLocal;
                        intervalLocal = newInterval;
                    }
                }

                if (_isRunning == 0)
                {
                    return;
                }
            }
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _isRunning, 0);
        }

        private static readonly float kTickFrequency = 1000f / Stopwatch.Frequency;

        private bool _isEnabled;
        private int _isRunning;
    }
}
