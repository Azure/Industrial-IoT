// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Plc
{
    using Opc.Ua;
    using Opc.Ua.Test;
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Timers;

    public class PlcSimulation
    {
        public static uint EventInstanceCount { get; set; } = 1;
        /// <summary>
        /// ms.
        /// </summary>
        public static uint EventInstanceRate { get; set; } = 1000;

        /// <summary>
        /// Simulation data.
        /// </summary>
        public static int SimulationCycleCount { get; set; } = kSimulationCycleCountDefault;
        public static int SimulationCycleLength { get; set; } = kSimulationCycleLengthDefault;

        /// <summary>
        /// Ctor for simulation server.
        /// </summary>
        /// <param name="plcNodeManager"></param>
        /// <param name="timeService"></param>
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

            PrintPublisherConfigJson();
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
                var e = new BaseEventState(null);
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
        /// <summary>
        /// Show and save pn.json
        /// </summary>
        public void PrintPublisherConfigJson()
        {
            var sb = new StringBuilder();

            sb.Append(Environment.NewLine)
                .AppendLine("[")
                .AppendLine("  {")
                .AppendLine("    \"EndpointUrl\": \"opc.tcp://localhost:{{Port}}/UA/SampleServer\",")
                .AppendLine("    \"UseSecurity\": true,")
                .AppendLine("    \"OpcNodes\": [");

            // Print config from plugin nodes list.
            foreach (var plugin in _plcNodeManager.PluginNodes)
            {
                foreach (var node in plugin.Nodes)
                {
                    // Show only if > 0 and != 1000 ms.
                    string publishingInterval = node.PublishingInterval > 0 &&
                                                node.PublishingInterval != 1000
                        ? $", \"OpcPublishingInterval\": {node.PublishingInterval}"
                        : string.Empty;
                    // Show only if > 0 ms.
                    string samplingInterval = node.SamplingInterval > 0
                        ? $", \"OpcSamplingInterval\": {node.SamplingInterval}"
                        : string.Empty;

                    string nodeId = JsonEncodedText.Encode(node.NodeId, JavaScriptEncoder.Default).ToString();
                    sb.Append("      { \"Id\": \"nsu=")
                        .Append(node.Namespace)
                        .Append(';')
                        .Append(node.NodeIdTypePrefix)
                        .Append('=')
                        .Append(nodeId)
                        .Append('\"')
                        .Append(publishingInterval)
                        .Append(samplingInterval)
                        .AppendLine(" },")
                        ;
                }
            }

            int trimLen = Environment.NewLine.Length + 1;
            sb
                .Remove(sb.Length - trimLen, trimLen)
                .Append(Environment.NewLine).AppendLine("    ]")
                .AppendLine("  }")
                .AppendLine("]"); // Trim trailing ,\n.

            string pnJson = sb.ToString();
            Console.Out.WriteLine(pnJson);
        }

        /// <summary>
        /// in cycles
        /// </summary>
        private const int kSimulationCycleCountDefault = 50;
        /// <summary>
        /// in msec
        /// </summary>
        private const int kSimulationCycleLengthDefault = 100;
        private readonly PlcNodeManager _plcNodeManager;
        private readonly TimeService _timeService;
        private ITimer _eventInstanceGenerator;
        private uint _eventInstanceCycle;
    }
}
