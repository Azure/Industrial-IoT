// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Plc.PluginNodes
{
    using Plc.PluginNodes.Models;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Test;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Node with a sine wave value with a spike anomaly.
    /// </summary>
    public class SpikePluginNode : IPluginNodes
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();
        public TimeService TimeService { get; set; }
        public ILogger Logger { get; set; }
        public uint ScaleUnits { get; set; }

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;
            var folder = _plcNodeManager.CreateFolder(
                telemetryFolder,
                path: "Anomaly",
                name: "Anomaly",
                NamespaceType.PlcApplications);
            AddNodes(folder);
        }

        public void StartSimulation()
        {
            _spikeCycleInPhase = PlcSimulation.SimulationCycleCount;
#pragma warning disable CA5394 // Do not use insecure randomness
            _spikeAnomalyCycle = _random.Next(PlcSimulation.SimulationCycleCount);
#pragma warning restore CA5394 // Do not use insecure randomness
            Logger.LogTrace("First spike anomaly cycle: {SpikeAnomalyCycle}", _spikeAnomalyCycle);

            _node.Start(SpikeGenerator, PlcSimulation.SimulationCycleLength);
        }

        public void StopSimulation()
        {
            _node.Stop();
        }

        private void AddNodes(FolderState folder)
        {
            _node = _plcNodeManager.CreateVariableNode<double>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "SpikeData",
                    name: "SpikeData",
                    new NodeId((uint)BuiltInType.Double),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentRead,
                    "Value with random spikes",
                    NamespaceType.PlcApplications));

            Nodes = new List<NodeWithIntervals>
            {
                new() {
                    NodeId = "SpikeData",
                    Namespace = Plc.Namespaces.PlcApplications
                }
            };
        }

        /// <summary>
        /// Generates a sine wave with spikes at a random cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        /// <param name="value"></param>
        private double SpikeGenerator(double value)
        {
            // calculate next value
            double nextValue;
            if (_spikeCycleInPhase == _spikeAnomalyCycle)
            {
                // todo calculate
                nextValue = kSimulationMaxAmplitude * 10;
                Logger.LogTrace("Generate spike anomaly");
            }
            else
            {
                nextValue = kSimulationMaxAmplitude * Math.Sin(2 * Math.PI / PlcSimulation.SimulationCycleCount * _spikeCycleInPhase);
            }
            Logger.LogTrace("Spike cycle: {SpikeCycleInPhase} data: {NextValue}",
                _spikeCycleInPhase, nextValue);

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_spikeCycleInPhase == 0)
            {
                _spikeCycleInPhase = PlcSimulation.SimulationCycleCount;
#pragma warning disable CA5394 // Do not use insecure randomness
                _spikeAnomalyCycle = _random.Next(PlcSimulation.SimulationCycleCount);
#pragma warning restore CA5394 // Do not use insecure randomness
                Logger.LogTrace("Next spike anomaly cycle: {SpikeAnomalyCycle}", _spikeAnomalyCycle);
            }

            return nextValue;
        }

        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<double> _node;
        private readonly Random _random = new();
        private int _spikeCycleInPhase;
        private int _spikeAnomalyCycle;
        private const double kSimulationMaxAmplitude = 100.0;
    }
}
