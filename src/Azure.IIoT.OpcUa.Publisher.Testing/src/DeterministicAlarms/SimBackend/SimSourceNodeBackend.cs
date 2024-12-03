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

    public class SimAlarmStateBackendEventArgs : EventArgs
    {
        public SimAlarmStateBackend SnapshotAlarm { get; set; }
    }

    public class SimSourceNodeBackend
    {
        public Dictionary<string, SimAlarmStateBackend> Alarms { get; } = [];

        public string Name { get; internal set; }

        public event EventHandler<SimAlarmStateBackendEventArgs> OnAlarmChanged;

        public void CreateAlarms(IList<Alarm> alarmsFromConfiguration)
        {
            foreach (var alarmConfiguration in alarmsFromConfiguration)
            {
                var alarmStateBackend = new SimAlarmStateBackend
                {
                    Source = this,
                    Id = alarmConfiguration.Id,
                    Name = alarmConfiguration.Name,
                    Time = DateTime.UtcNow,
                    AlarmType = alarmConfiguration.ObjectType
                };

                lock (Alarms)
                {
                    Alarms.Add(alarmConfiguration.Id, alarmStateBackend);
                }
            }
        }

        internal void Refresh()
        {
            var snapshots = new List<SimAlarmStateBackend>();

            lock (Alarms)
            {
                foreach (var alarm in Alarms.Values)
                {
                    snapshots.Add(alarm.CreateSnapshot());
                }
            }

            foreach (var snapshotAlarm in snapshots)
            {
                OnAlarmChanged!.Invoke(this, new SimAlarmStateBackendEventArgs { SnapshotAlarm = snapshotAlarm });
            }
        }
    }
}
