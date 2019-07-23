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

namespace Alarms {
    using System;
    using Opc.Ua;

    /// <summary>
    /// This class stores the state of a alarm known to the system.
    /// </summary>
    /// <remarks>
    /// This class only stores the information about an alarm that a system has. The
    /// system has no concept of the UA information model and the NodeManager must
    /// convert the information stored in this class into the UA equivalent.
    /// </remarks>
    public class UnderlyingSystemAlarm {

        /// <summary>
        /// The source that the alarm belongs to
        /// </summary>
        /// <value>The source.</value>
        public UnderlyingSystemSource Source { get; set; }

        /// <summary>
        /// Gets or sets the name of the alarm.
        /// </summary>
        /// <value>The name of the alarm.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the alarm.
        /// </summary>
        /// <value>The type of the alarm.</value>
        public string AlarmType { get; set; }

        /// <summary>
        /// Gets or sets a unique record number assigned to an archived snapshot of the alarm.
        /// </summary>
        /// <value>The record number assigned to an archived snapshot of the alarm.</value>
        /// <remarks>
        /// Past state transitions are assigned a record number when they are archived. This
        /// Record number allows the system to updated archived record. This number is 0 if
        /// the state transition has not been archived.
        /// </remarks>
        public uint RecordNumber { get; set; }

        /// <summary>
        /// Gets or sets the time when the alarm last changed state.
        /// </summary>
        /// <value>The last state change time.</value>
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets the reason for the last state change.
        /// </summary>
        /// <value>The reason for the last state change.</value>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the severity of the alarm.
        /// </summary>
        /// <value>The alarm severity.</value>
        public EventSeverity Severity { get; set; }

        /// <summary>
        /// Gets or sets the comment associated with the alarm.
        /// </summary>
        /// <value>The comment.</value>
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets the name of the user that provided the comment.
        /// </summary>
        /// <value>The name of the user that provided the comment.</value>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the current alarm state.
        /// </summary>
        /// <value>The current alarm state.</value>
        public UnderlyingSystemAlarmStates State { get; set; }

        /// <summary>
        /// Gets or sets the time when the alarm went into the enabled state.
        /// </summary>
        /// <value>When the alarm went into the enabled state.</value>
        public DateTime EnableTime { get; set; }

        /// <summary>
        /// Gets or sets the time when the alarm went into the active state.
        /// </summary>
        /// <value>When the alarm went into the active state.</value>
        public DateTime ActiveTime { get; set; }

        /// <summary>
        /// Gets or sets the limits that apply to the alarm.
        /// </summary>
        /// <value>The limits that apply to the alarm.</value>
        /// <remarks>
        /// 1 limit = High
        /// 2 limits = High, Low
        /// 4 limits = HighHigh, High, Low, LowLow
        /// </remarks>
        public double[] Limits { get; set; }

        /// <summary>
        /// Creates a snapshort of the alarm.
        /// </summary>
        /// <returns>The snapshot,</returns>
        public UnderlyingSystemAlarm CreateSnapshot() {
            return (UnderlyingSystemAlarm)MemberwiseClone();
        }

        /// <summary>
        /// Sets or clears the bits in the alarm state mask.
        /// </summary>
        /// <param name="bits">The bits.</param>
        /// <param name="isSet">if set to <c>true</c> the bits are set; otherwise they are cleared.</param>
        /// <returns>True if the state changed as a result of setting the bits.</returns>
        public bool SetStateBits(UnderlyingSystemAlarmStates bits, bool isSet) {
            if (isSet) {
                var currentlySet = (State & bits) == bits;
                State |= bits;
                return !currentlySet;
            }

            var currentlyCleared = (State & ~bits) == State;
            State &= ~bits;
            return !currentlyCleared;
        }
    }
}
