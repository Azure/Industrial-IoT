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
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public class SimBackendService
    {
        private readonly Lock _lock = new();
        public Dictionary<string, SimSourceNodeBackend> SourceNodes { get; } = [];

        public SimSourceNodeBackend CreateSourceNodeBackend(string name,
            IList<Alarm> alarms, EventHandler<SimAlarmStateBackendEventArgs> alarmChangeCallback)
        {
            SimSourceNodeBackend simSourceNodeBackend;

            lock (_lock)
            {
                simSourceNodeBackend = new SimSourceNodeBackend
                {
                    Name = name
                };

                simSourceNodeBackend.OnAlarmChanged += alarmChangeCallback;
                simSourceNodeBackend.CreateAlarms(alarms);

                SourceNodes[name] = simSourceNodeBackend;
            }

            return simSourceNodeBackend;
        }
    }
}
