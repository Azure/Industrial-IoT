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
    /// Node with a sine wave value with a dip anomaly.
    /// </summary>
    public class DipPluginNode : IPluginNodes
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
            _dipCycleInPhase = PlcSimulation.SimulationCycleCount;
#pragma warning disable CA5394 // Do not use insecure randomness
            _dipAnomalyCycle = _random.Next(PlcSimulation.SimulationCycleCount);
#pragma warning restore CA5394 // Do not use insecure randomness
            Logger.LogTrace("First dip anomaly cycle: {DipAnomalyCycle}", _dipAnomalyCycle);

            _node.Start(DipGenerator, PlcSimulation.SimulationCycleLength);
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
                    path: "DipData",
                    name: "DipData",
                    new NodeId((uint)BuiltInType.Double),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentRead,
                    "Value with random dips",
                    NamespaceType.PlcApplications));

            Nodes = new List<NodeWithIntervals>
            {
                new() {
                    NodeId = "DipData",
                    Namespace = Plc.Namespaces.PlcApplications
                }
            };
        }

        /// <summary>
        /// Generates a sine wave with dips at a random cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        /// <param name="value"></param>
        private double DipGenerator(double value)
        {
            // calculate next value
            double nextValue;
            if (_dipCycleInPhase == _dipAnomalyCycle)
            {
                nextValue = kSimulationMaxAmplitude * -10;
                Logger.LogTrace("Generate dip anomaly");
            }
            else
            {
                nextValue = kSimulationMaxAmplitude * Math.Sin(2 * Math.PI / PlcSimulation.SimulationCycleCount * _dipCycleInPhase);
            }
            Logger.LogTrace("Spike cycle: {DipCycleInPhase} data: {NextValue}", _dipCycleInPhase, nextValue);

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_dipCycleInPhase == 0)
            {
                _dipCycleInPhase = PlcSimulation.SimulationCycleCount;
#pragma warning disable CA5394 // Do not use insecure randomness
                _dipAnomalyCycle = _random.Next(PlcSimulation.SimulationCycleCount);
#pragma warning restore CA5394 // Do not use insecure randomness
                Logger.LogTrace("Next dip anomaly cycle: {AnomalyCycle}", _dipAnomalyCycle);
            }

            return nextValue;
        }
        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<double> _node;
        private readonly Random _random = new();
        private int _dipCycleInPhase;
        private int _dipAnomalyCycle;
        private const double kSimulationMaxAmplitude = 100.0;
    }
}
