namespace Plc
{
    using Opc.Ua;
    using Opc.Ua.Test;
    using System.Diagnostics;
    using System.Timers;

    public class PlcSimulation
    {
        public static uint EventInstanceCount { get; set; } = 1;
        public static uint EventInstanceRate { get; set; } = 1000; // ms.

        /// <summary>
        /// Simulation data.
        /// </summary>
        public static int SimulationCycleCount { get; set; } = kSimulationCycleCountDefault;
        public static int SimulationCycleLength { get; set; } = kSimulationCycleLengthDefault;

        /// <summary>
        /// Ctor for simulation server.
        /// </summary>
        public PlcSimulation(PlcNodeManager plcNodeManager, TimeService timeService)
        {
            _plcNodeManager = plcNodeManager;
            _timeService = timeService;
        }

        /// <summary>
        /// Start the simulation.
        /// </summary>
        public void Start()
        {
            if (EventInstanceCount > 0)
            {
                _eventInstanceGenerator = EventInstanceRate >= 50 || !Stopwatch.IsHighResolution
                    ? _timeService.NewTimer(UpdateEventInstances, EventInstanceRate)
                    : _timeService.NewFastTimer(UpdateVeryFastEventInstances, EventInstanceRate);
            }

            // Start simulation of nodes from plugin nodes list.
            foreach (var plugin in _plcNodeManager.PluginNodes)
            {
                plugin.StartSimulation();
            }
        }

        /// <summary>
        /// Stop the simulation.
        /// </summary>
        public void Stop()
        {
            Disable(_eventInstanceGenerator);

            // Stop simulation of nodes from plugin nodes list.
            foreach (var plugin in _plcNodeManager.PluginNodes)
            {
                plugin.StopSimulation();
            }
        }

        private void UpdateEventInstances(object state, ElapsedEventArgs elapsedEventArgs)
        {
            UpdateEventInstances();
        }

        private void UpdateVeryFastEventInstances(object state, FastTimerElapsedEventArgs elapsedEventArgs)
        {
            UpdateEventInstances();
        }

        private void UpdateEventInstances()
        {
            var eventInstanceCycle = _eventInstanceCycle++;

            for (uint i = 0; i < EventInstanceCount; i++)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                var e = new BaseEventState(null);
#pragma warning restore CA2000 // Dispose objects before losing scope
                var info = new TranslationInfo(
                    "EventInstanceCycleEventKey",
                    "en-us",
                    "Event with index '{0}' and event cycle '{1}'",
                    i, eventInstanceCycle);

                e.Initialize(
                    _plcNodeManager.SystemContext,
                    source: null,
                    EventSeverity.Medium,
                    new LocalizedText(info));

                e.SetChildValue(_plcNodeManager.SystemContext, BrowseNames.SourceName, "System", false);
                e.SetChildValue(_plcNodeManager.SystemContext, BrowseNames.SourceNode, ObjectIds.Server, false);

                _plcNodeManager.Server.ReportEvent(e);
            }
        }

        private static void Disable(ITimer timer)
        {
            if (timer == null)
            {
                return;
            }

            timer.Enabled = false;
        }

        private const int kSimulationCycleCountDefault = 50;          // in cycles
        private const int kSimulationCycleLengthDefault = 100;        // in msec
        private readonly PlcNodeManager _plcNodeManager;
        private readonly TimeService _timeService;
        private ITimer _eventInstanceGenerator;
        private uint _eventInstanceCycle;
    }
}
