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
    using System.Timers;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Service returning <see cref="DateTime"/> values and <see cref="Timer"/> instances. Mocked in tests.
    /// </summary>
    public class TimeService
    {
        /// <summary>
        /// Create a new <see cref="Timer"/> instance with <see cref="Timer.Enabled"/> set to true
        /// and <see cref="Timer.AutoReset"/> set to true. The <see cref="Timer"/> will call the
        /// provided callback at regular intervals. This method is overridden in tests to return
        /// a mock object.
        /// </summary>
        /// <param name="callback">Event handler to call at regular intervals.</param>
        /// <param name="intervalInMilliseconds">Time interval at which to call the callback.</param>
        /// <returns>A <see cref="Timer"/>.</returns>
        public virtual ITimer NewTimer(
            ElapsedEventHandler callback,
            uint intervalInMilliseconds)
        {
            var timer = new TimerAdapter
            {
                Interval = intervalInMilliseconds,
                AutoReset = true,
                Enabled = true
            };
            timer.Elapsed += callback;
            return timer;
        }

        /// <summary>
        /// Create a new <see cref="FastTimer"/> instance with <see cref="FastTimer.Enabled"/> set to true
        /// and <see cref="FastTimer.AutoReset"/> set to true. The <see cref="FastTimer"/> will call the
        /// provided callback at regular intervals. This method is overridden in tests to return
        /// a mock object.
        /// </summary>
        /// <param name="callback">Event handler to call at regular intervals.</param>
        /// <param name="intervalInMilliseconds">Time interval at which to call the callback.</param>
        /// <returns>A <see cref="Timer"/>.</returns>
        public virtual ITimer NewFastTimer(
            EventHandler<FastTimerElapsedEventArgs> callback,
            uint intervalInMilliseconds)
        {
            var timer = new FastTimer
            {
                Interval = intervalInMilliseconds,
                AutoReset = true
            };
            timer.Elapsed += callback;
            timer.Enabled = true;
            return timer;
        }

        /// <summary>
        /// Returns the current time. Overridden in tests.
        /// </summary>
        /// <returns>The current time.</returns>
        public virtual DateTime Now => DateTime.Now;

        /// <summary>
        /// Returns the current UTC time. Overridden in tests.
        /// </summary>
        /// <returns>The current UTC time.</returns>
        public virtual DateTime UtcNow => DateTime.UtcNow;

        /// <summary>
        /// An adapter allowing the construction of <see cref="Timer"/> objects
        /// that explicitly implement the <see cref="ITimer"/> interface.
        /// The adapter itself must remain empty, add any required properties
        /// or methods from the <see cref="Timer"/> class into the
        /// <see cref="ITimer"/> interface.
        /// </summary>
        private sealed class TimerAdapter : Timer, ITimer;
    }
}
