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
    using System.Threading;

    /// <summary>
    /// An object that provides access to the underlying system.
    /// </summary>
    public class UnderlyingSystem : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnderlyingSystem"/> class.
        /// </summary>
        /// <param name="timeService"></param>
        public UnderlyingSystem(TimeService timeService)
        {
            _sources = [];
            _timeService = timeService;
        }

        /// <summary>
        /// The finializer implementation.
        /// </summary>
        ~UnderlyingSystem()
        {
            Dispose(false);
        }

        /// <summary>
        /// Frees any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _simulationTimer != null)
            {
                _simulationTimer.Dispose();
                _simulationTimer = null;
            }
        }

        /// <summary>
        /// Creates a source.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="alarmChangeCallback">The callback invoked when an alarm changes.</param>
        /// <returns>The source.</returns>
        public UnderlyingSystemSource CreateSource(string sourcePath, AlarmChangedEventHandler alarmChangeCallback)
        {
            UnderlyingSystemSource source = null;

            lock (_lock)
            {
                // create a new source.
                source = new UnderlyingSystemSource(_timeService);

                // extract the name from the path.
                var name = sourcePath;

                var index = name.LastIndexOf('/');

                if (index != -1)
                {
                    name = name[(index + 1)..];
                }

                // extract the type from the path.
                var type = sourcePath;

                index = type.IndexOf('/');

                if (index != -1)
                {
                    type = type[..index];
                }

                // create the source.
                source.SourcePath = sourcePath;
                source.Name = name;
                source.SourceType = type;
                source.OnAlarmChanged = alarmChangeCallback;

                _sources.Add(sourcePath, source);
            }

            // add the alarms based on the source type.
            // note that the source and alarm types used here are types defined by the underlying system.
            // the node manager will need to map these types to UA defined types.
            switch (source.SourceType)
            {
                case "Colours":
                    {
                        source.CreateAlarm("Red", "HighAlarm");
                        source.CreateAlarm("Yellow", "HighLowAlarm");
                        source.CreateAlarm("Green", "TripAlarm");
                        break;
                    }

                case "Metals":
                    {
                        source.CreateAlarm("Gold", "HighAlarm");
                        source.CreateAlarm("Silver", "HighLowAlarm");
                        source.CreateAlarm("Bronze", "TripAlarm");
                        break;
                    }
            }

            // return the new source.
            return source;
        }

        /// <summary>
        /// Starts a simulation which causes the alarm states to change.
        /// </summary>
        /// <remarks>
        /// This simulation randomly activates the alarms that belong to the sources.
        /// Once an alarm is active it has to be acknowledged and confirmed.
        /// Once an alarm is confirmed it go to the inactive state.
        /// If the alarm stays active the severity will be gradually increased.
        /// </remarks>
        public void StartSimulation()
        {
            lock (_lock)
            {
                if (_simulationTimer != null)
                {
                    _simulationTimer.Dispose();
                    _simulationTimer = null;
                }

                _simulationTimer = new Timer(DoSimulation, null, 1000, 1000);
            }
        }

        /// <summary>
        /// Stops the simulation.
        /// </summary>
        public void StopSimulation()
        {
            lock (_lock)
            {
                if (_simulationTimer != null)
                {
                    _simulationTimer.Dispose();
                    _simulationTimer = null;
                }
            }
        }

        /// <summary>
        /// Simulates a source by updating the state of the alarms belonging to the condition.
        /// </summary>
        /// <param name="state"></param>
        private void DoSimulation(object state)
        {
            try
            {
                // get the list of sources.
                List<UnderlyingSystemSource> sources = null;

                lock (_lock)
                {
                    _simulationCounter++;
                    sources = new List<UnderlyingSystemSource>(_sources.Values);
                }

                // run simulation for each source.
                for (var ii = 0; ii < sources.Count; ii++)
                {
                    sources[ii].DoSimulation(_simulationCounter, ii);
                }
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Unexpected error running simulation for system");
            }
        }

        private readonly Lock _lock = new();
        private readonly Dictionary<string, UnderlyingSystemSource> _sources;
        private readonly TimeService _timeService;
        private Timer _simulationTimer;
        private long _simulationCounter;
    }
}
