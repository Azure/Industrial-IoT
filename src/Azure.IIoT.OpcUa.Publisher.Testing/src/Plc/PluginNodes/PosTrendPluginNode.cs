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
    /// Node with a value that shows a positive trend.
    /// </summary>
    public class PosTrendPluginNode : IPluginNodes
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
            AddMethods(methodsFolder);
        }

        public void StartSimulation()
        {
#pragma warning disable CA5394 // Do not use insecure randomness
            _posTrendAnomalyPhase = _random.Next(10);
#pragma warning restore CA5394 // Do not use insecure randomness
            _posTrendCycleInPhase = PlcSimulation.SimulationCycleCount;
            Logger.LogTrace("First pos trend anomaly phase: {PosTrendAnomalyPhase}", _posTrendAnomalyPhase);

            _node.Start(PosTrendGenerator, PlcSimulation.SimulationCycleLength);
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
                    path: "PositiveTrendData",
                    name: "PositiveTrendData",
                    new NodeId((uint)BuiltInType.Double),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentRead,
                    "Value with a slow positive trend",
                    NamespaceType.PlcApplications));

            Nodes = new List<NodeWithIntervals>
            {
                new() {
                    NodeId = "PositiveTrendData",
                    Namespace = Plc.Namespaces.PlcApplications
                }
            };
        }

        private void AddMethods(FolderState methodsFolder)
        {
            var resetTrendMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: "ResetPosTrend",
                name: "ResetPosTrend",
                "Reset the positive trend values to their baseline value",
                NamespaceType.PlcApplications);
            SetResetTrendMethodProperties(ref resetTrendMethod);
        }

        /// <summary>
        /// Generates a sine wave with spikes at a configurable cycle in the phase.
        /// Called each SimulationCycleLength msec.
        /// </summary>
        /// <param name="value"></param>
        private double PosTrendGenerator(double value)
        {
            // calculate next value
            var nextValue = kTrendBaseValue;
            if (_posTrendPhase >= _posTrendAnomalyPhase)
            {
                nextValue = kTrendBaseValue + ((_posTrendPhase - _posTrendAnomalyPhase) / 10d);
                Logger.LogTrace("Generate postrend anomaly");
            }

            // end of cycle: reset cycle count and calc next anomaly cycle
            if (--_posTrendCycleInPhase == 0)
            {
                _posTrendCycleInPhase = PlcSimulation.SimulationCycleCount;
                _posTrendPhase++;
                Logger.LogTrace("Pos trend phase: {PosTrendPhase}, data: {NextValue}",
                    _posTrendPhase, nextValue);
            }
            return nextValue;
        }

        private void SetResetTrendMethodProperties(ref MethodState method)
        {
            method.OnCallMethod += OnResetTrendCall;
        }

        /// <summary>
        /// Method to reset the trend values. Executes synchronously.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        private ServiceResult OnResetTrendCall(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            ResetTrendData();
            Logger.LogDebug("ResetPosTrend method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method implementation to reset the trend data.
        /// </summary>
        public void ResetTrendData()
        {
#pragma warning disable CA5394 // Do not use insecure randomness
            _posTrendAnomalyPhase = _random.Next(10);
#pragma warning restore CA5394 // Do not use insecure randomness
            _posTrendCycleInPhase = PlcSimulation.SimulationCycleCount;
            _posTrendPhase = 0;
        }

        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<double> _node;
        private readonly Random _random = new();
        private int _posTrendCycleInPhase;
        private int _posTrendPhase;
        private int _posTrendAnomalyPhase;
        private const double kTrendBaseValue = 100.0;
    }
}
