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

namespace Alarms
{
    using Opc.Ua;
    using Opc.Ua.Test;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class simulates a source in the system.
    /// </summary>
    public class UnderlyingSystemSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnderlyingSystemSource"/> class.
        /// </summary>
        /// <param name="timeService"></param>
        public UnderlyingSystemSource(TimeService timeService)
        {
            _alarms = [];
            _archive = [];
            _timeService = timeService;
        }

        /// <summary>
        /// Used to receive events when the state of an alarm changed.
        /// </summary>
        public AlarmChangedEventHandler OnAlarmChanged { get; set; }

        /// <summary>
        /// Gets or sets the name of the source.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the fully qualified name for the source.
        /// </summary>
        /// <value>The fully qualified name for a source.</value>
        public string SourcePath { get; set; }

        /// <summary>
        /// Gets or sets the type of the source.
        /// </summary>
        /// <value>The type of the source.</value>
        public string SourceType { get; set; }

        /// <summary>
        /// Creates a new active alarm for the source.
        /// </summary>
        /// <param name="alarmName">Name of the alarm.</param>
        /// <param name="alarmType">Type of the alarm.</param>
        public void CreateAlarm(string alarmName, string alarmType)
        {
            var alarm = new UnderlyingSystemAlarm
            {
                Source = this,
                Name = alarmName,
                AlarmType = alarmType,
                RecordNumber = 0,
                Reason = "Alarm created.",
                Time = _timeService.UtcNow,
                Severity = EventSeverity.Low,
                Comment = null,
                UserName = null,
                State = UnderlyingSystemAlarmStates.Active | UnderlyingSystemAlarmStates.Enabled,
                EnableTime = _timeService.UtcNow,
                ActiveTime = _timeService.UtcNow
            };

            switch (alarmType)
            {
                case "HighAlarm":
                    {
                        alarm.Limits = [80];
                        alarm.State |= UnderlyingSystemAlarmStates.High;
                        break;
                    }

                case "HighLowAlarm":
                    {
                        alarm.Limits = [90, 70, 30, 10];
                        alarm.State |= UnderlyingSystemAlarmStates.High;
                        break;
                    }
            }

            lock (_alarms)
            {
                _alarms.Add(alarm);
            }
        }

        /// <summary>
        /// Enables or disables the alarm.
        /// </summary>
        /// <param name="alarmName">Name of the alarm.</param>
        /// <param name="enabling">if set to <c>true</c> the alarm is enabled.</param>
        public void EnableAlarm(string alarmName, bool enabling)
        {
            var snapshots = new List<UnderlyingSystemAlarm>();

            lock (_alarms)
            {
                var alarm = FindAlarm(alarmName, 0);

                if (alarm != null)
                {
                    // enable/disable the alarm.
                    if (alarm.SetStateBits(UnderlyingSystemAlarmStates.Enabled, enabling))
                    {
                        alarm.Time = alarm.EnableTime = _timeService.UtcNow;
                        alarm.Reason = "The alarm was " + (enabling ? "enabled." : "disabled.");
                        snapshots.Add(alarm.CreateSnapshot());
                    }

                    // enable/disable any archived records for the alarm.
                    foreach (var record in _archive.Values)
                    {
                        if (record.Name != alarmName)
                        {
                            continue;
                        }

                        if (record.SetStateBits(UnderlyingSystemAlarmStates.Enabled, enabling))
                        {
                            record.Time = alarm.EnableTime = _timeService.UtcNow;
                            record.Reason = "The alarm was " + (enabling ? "enabled." : "disabled.");
                            snapshots.Add(alarm.CreateSnapshot());
                        }
                    }
                }
            }

            // report any alarm changes after releasing the lock.
            for (var ii = 0; ii < snapshots.Count; ii++)
            {
                ReportAlarmChange(snapshots[ii]);
            }
        }

        /// <summary>
        /// Adds a comment to an alarm.
        /// </summary>
        /// <param name="alarmName">Name of the alarm.</param>
        /// <param name="recordNumber">The record number.</param>
        /// <param name="comment">The comment.</param>
        /// <param name="userName">Name of the user.</param>
        public void CommentAlarm(string alarmName, uint recordNumber, LocalizedText comment, string userName)
        {
            UnderlyingSystemAlarm snapshot = null;

            lock (_alarms)
            {
                var alarm = FindAlarm(alarmName, recordNumber);

                if (alarm != null)
                {
                    alarm.Time = _timeService.UtcNow;
                    alarm.Reason = "A comment was added.";
                    alarm.UserName = userName;

                    // only change the comment if a non-null comment was provided.
                    if (comment != null && (!string.IsNullOrEmpty(comment.Text) || !string.IsNullOrEmpty(comment.Locale)))
                    {
                        alarm.Comment = Utils.Format("{0}", comment);
                    }

                    snapshot = alarm.CreateSnapshot();
                }
            }

            if (snapshot != null)
            {
                ReportAlarmChange(snapshot);
            }
        }

        /// <summary>
        /// Acknowledges an alarm.
        /// </summary>
        /// <param name="alarmName">Name of the alarm.</param>
        /// <param name="recordNumber">The record number.</param>
        /// <param name="comment">The comment.</param>
        /// <param name="userName">Name of the user.</param>
        public void AcknowledgeAlarm(string alarmName, uint recordNumber, LocalizedText comment, string userName)
        {
            UnderlyingSystemAlarm snapshot = null;

            lock (_alarms)
            {
                var alarm = FindAlarm(alarmName, recordNumber);

                if (alarm != null)
                {
                    if (alarm.SetStateBits(UnderlyingSystemAlarmStates.Acknowledged, true))
                    {
                        alarm.Time = _timeService.UtcNow;
                        alarm.Reason = "The alarm was acknoweledged.";
                        alarm.Comment = Utils.Format("{0}", comment);
                        alarm.UserName = userName;

                        alarm.SetStateBits(UnderlyingSystemAlarmStates.Confirmed, false);
                    }

                    snapshot = alarm.CreateSnapshot();
                }
            }

            if (snapshot != null)
            {
                ReportAlarmChange(snapshot);
            }
        }

        /// <summary>
        /// Confirms an alarm.
        /// </summary>
        /// <param name="alarmName">Name of the alarm.</param>
        /// <param name="recordNumber">The record number.</param>
        /// <param name="comment">The comment.</param>
        /// <param name="userName">Name of the user.</param>
        public void ConfirmAlarm(string alarmName, uint recordNumber, LocalizedText comment, string userName)
        {
            UnderlyingSystemAlarm snapshot = null;

            lock (_alarms)
            {
                var alarm = FindAlarm(alarmName, recordNumber);

                if (alarm != null)
                {
                    if (alarm.SetStateBits(UnderlyingSystemAlarmStates.Confirmed, true))
                    {
                        alarm.Time = _timeService.UtcNow;
                        alarm.Reason = "The alarm was confirmed.";
                        alarm.Comment = Utils.Format("{0}", comment);
                        alarm.UserName = userName;

                        // remove branch.
                        if (recordNumber != 0)
                        {
                            _archive.Remove(recordNumber);
                            alarm.SetStateBits(UnderlyingSystemAlarmStates.Deleted, true);
                        }

                        // de-activate alarm.
                        else
                        {
                            alarm.SetStateBits(UnderlyingSystemAlarmStates.Active, false);
                        }
                    }

                    snapshot = alarm.CreateSnapshot();
                }
            }

            if (snapshot != null)
            {
                ReportAlarmChange(snapshot);
            }
        }

        /// <summary>
        /// Reports the current state of all conditions.
        /// </summary>
        public void Refresh()
        {
            var snapshots = new List<UnderlyingSystemAlarm>();

            lock (_alarms)
            {
                for (var ii = 0; ii < _alarms.Count; ii++)
                {
                    var alarm = _alarms[ii];
                    snapshots.Add(alarm.CreateSnapshot());
                }
            }

            // report any alarm changes after releasing the lock.
            for (var ii = 0; ii < snapshots.Count; ii++)
            {
                ReportAlarmChange(snapshots[ii]);
            }
        }

        /// <summary>
        /// Sets the state of the source (surpresses any active alarms).
        /// </summary>
        /// <param name="offline">if set to <c>true</c> the source is offline.</param>
        public void SetOfflineState(bool offline)
        {
            IsOffline = offline;
            var snapshots = new List<UnderlyingSystemAlarm>();

            lock (_alarms)
            {
                for (var ii = 0; ii < _alarms.Count; ii++)
                {
                    var alarm = _alarms[ii];

                    if (alarm.SetStateBits(UnderlyingSystemAlarmStates.Suppressed, offline))
                    {
                        alarm.Time = alarm.EnableTime = _timeService.UtcNow;
                        alarm.Reason = "The alarm was " + (offline ? "suppressed." : "unsuppressed.");

                        // check if the alarm change should be reported.
                        if ((alarm.State & UnderlyingSystemAlarmStates.Enabled) != 0)
                        {
                            snapshots.Add(alarm.CreateSnapshot());
                        }
                    }
                }
            }

            // report any alarm changes after releasing the lock.
            for (var ii = 0; ii < snapshots.Count; ii++)
            {
                ReportAlarmChange(snapshots[ii]);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the source is offline.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is offline; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// All alarms for offline sources are suppressed.
        /// </remarks>
        public bool IsOffline { get; private set; }

        /// <summary>
        /// Simulates a source by updating the state of the alarms belonging to the condition.
        /// </summary>
        /// <param name="counter">The number of simulation cycles that have elapsed.</param>
        /// <param name="index">The index of the source within the system.</param>
        public void DoSimulation(long counter, int index)
        {
            try
            {
                var snapshots = new List<UnderlyingSystemAlarm>();

                // update the alarms.
                lock (_alarms)
                {
                    for (var ii = 0; ii < _alarms.Count; ii++)
                    {
                        UpdateAlarm(_alarms[ii], counter, ii + index, snapshots);
                    }
                }

                // report any alarm changes after releasing the lock.
                for (var ii = 0; ii < snapshots.Count; ii++)
                {
                    ReportAlarmChange(snapshots[ii]);
                }
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Unexpected error running simulation for source {0}", SourcePath);
            }
        }

        /// <summary>
        /// Finds the alarm identified by the name.
        /// </summary>
        /// <param name="alarmName">Name of the alarm.</param>
        /// <param name="recordNumber">The record number associated with the alarm.</param>
        /// <returns>The alarm if null; otherwise null.</returns>
        private UnderlyingSystemAlarm FindAlarm(string alarmName, uint recordNumber)
        {
            lock (_alarms)
            {
                // look up archived alarm.
                if (recordNumber != 0)
                {
                    if (!_archive.TryGetValue(recordNumber, out var alarm))
                    {
                        return null;
                    }

                    return alarm;
                }

                // look up alarm.
                for (var ii = 0; ii < _alarms.Count; ii++)
                {
                    var alarm = _alarms[ii];

                    if (alarm.Name == alarmName)
                    {
                        return alarm;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Reports a change to an alarm record.
        /// </summary>
        /// <param name="alarm">The alarm.</param>
        private void ReportAlarmChange(UnderlyingSystemAlarm alarm)
        {
            if (OnAlarmChanged != null)
            {
                try
                {
                    OnAlarmChanged(alarm);
                }
                catch (Exception e)
                {
                    Utils.Trace(e, "Unexpected error reporting change to an Alarm for Source {0}.", SourcePath);
                }
            }
        }

        /// <summary>
        /// Updates the state of an alarm.
        /// </summary>
        /// <param name="alarm"></param>
        /// <param name="counter"></param>
        /// <param name="index"></param>
        /// <param name="snapshots"></param>
        private void UpdateAlarm(UnderlyingSystemAlarm alarm, long counter, int index, List<UnderlyingSystemAlarm> snapshots)
        {
            string reason = null;

            // ignore disabled alarms.
            if ((alarm.State & UnderlyingSystemAlarmStates.Enabled) == 0)
            {
                return;
            }

            // check if the alarm needs to be updated this cycle.
            if (counter % (8 + (index % 4)) == 0)
            {
                // check if it is time to activate.
                if ((alarm.State & UnderlyingSystemAlarmStates.Active) == 0)
                {
                    reason = "The alarm is active.";

                    alarm.SetStateBits(UnderlyingSystemAlarmStates.Active, true);
                    alarm.SetStateBits(UnderlyingSystemAlarmStates.Acknowledged | UnderlyingSystemAlarmStates.Confirmed, false);
                    alarm.Severity = EventSeverity.Low;
                    alarm.ActiveTime = _timeService.UtcNow;

                    switch (alarm.AlarmType)
                    {
                        case "HighAlarm":
                            {
                                alarm.SetStateBits(UnderlyingSystemAlarmStates.Limits, false);
                                alarm.SetStateBits(UnderlyingSystemAlarmStates.High, true);
                                break;
                            }

                        case "HighLowAlarm":
                            {
                                alarm.SetStateBits(UnderlyingSystemAlarmStates.Limits, false);
                                alarm.SetStateBits(UnderlyingSystemAlarmStates.Low, true);
                                break;
                            }
                    }
                }

                // bump the severity.
                else if ((alarm.State & UnderlyingSystemAlarmStates.Acknowledged) == 0)
                {
                    if (alarm.Severity < EventSeverity.High)
                    {
                        reason = "The alarm severity has increased.";

                        var values = Enum.GetValues<EventSeverity>();

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            var severity = (EventSeverity)values.GetValue(ii);

                            if (severity > alarm.Severity)
                            {
                                alarm.Severity = severity;
                                break;
                            }
                        }

                        if (alarm.Severity > EventSeverity.Medium)
                        {
                            switch (alarm.AlarmType)
                            {
                                case "HighLowAlarm":
                                    {
                                        alarm.SetStateBits(UnderlyingSystemAlarmStates.Limits, false);
                                        alarm.SetStateBits(UnderlyingSystemAlarmStates.LowLow, true);
                                        break;
                                    }
                            }
                        }
                    }

                    // give up on the alarm.
                    else
                    {
                        // create an archived state that needs to be acknowledged.
                        if (alarm.AlarmType == "TripAlarm")
                        {
                            // check the number of archived states.
                            var count = 0;

                            foreach (var record in _archive.Values)
                            {
                                if (record.Name == alarm.Name)
                                {
                                    count++;
                                }
                            }
                            // limit the number of archived states to avoid filling up the display.
                            if (count < 2)
                            {
                                // archive the current state.
                                var snapshot = alarm.CreateSnapshot();
                                snapshot.RecordNumber = ++_nextRecordNumber;
                                snapshot.Severity = EventSeverity.Low;
                                _archive.Add(snapshot.RecordNumber, snapshot);
                                snapshots.Add(snapshot);
                            }
                        }

                        reason = "The alarm was deactivated by the system.";
                        alarm.SetStateBits(UnderlyingSystemAlarmStates.Active, false);
                        //alarm.SetStateBits(UnderlyingSystemAlarmStates.Acknowledged | UnderlyingSystemAlarmStates.Confirmed, true);
                        alarm.Severity = EventSeverity.Low;
                    }
                }
            }

            // update the reason.
            if (reason != null)
            {
                alarm.Time = _timeService.UtcNow;
                alarm.Reason = reason;

                // return a snapshot used to report the state change.
                snapshots.Add(alarm.CreateSnapshot());
            }

            // no change so nothing to report.
        }

        private readonly List<UnderlyingSystemAlarm> _alarms;
        private readonly Dictionary<uint, UnderlyingSystemAlarm> _archive;
        private readonly TimeService _timeService;
        private uint _nextRecordNumber;
    }

    /// <summary>
    /// Used to receive events when the state of an alarm changes.
    /// </summary>
    /// <param name="alarm"></param>
    public delegate void AlarmChangedEventHandler(UnderlyingSystemAlarm alarm);
}
