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

namespace DeterministicAlarms.SimBackend
{
    using DeterministicAlarms.Configuration;
    using DeterministicAlarms.Model;
    using Opc.Ua;
    using System;

    public class SimAlarmStateBackend
    {
        public string Name { get; internal set; }

        public AlarmObjectStates AlarmType { get; set; }

        public DateTime Time { get; internal set; }

        public string Reason { get; internal set; }

        public SimConditionStatesEnum State { get; internal set; }

        public LocalizedText Comment { get; internal set; }

        public string UserName { get; internal set; }

        public EventSeverity Severity { get; internal set; }

        public DateTime EnableTime { get; internal set; }

        public DateTime ActiveTime { get; internal set; }

        public SimSourceNodeBackend Source { get; internal set; }

        public string Id { get; internal set; }

        internal SimAlarmStateBackend CreateSnapshot()
        {
            return (SimAlarmStateBackend)MemberwiseClone();
        }

        internal bool SetStateBits(SimConditionStatesEnum bits, bool isSet)
        {
            if (isSet)
            {
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
