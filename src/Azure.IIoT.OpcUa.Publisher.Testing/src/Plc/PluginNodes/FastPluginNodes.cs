﻿namespace Plc.PluginNodes
{
    using Plc.PluginNodes.Models;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Test;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Timers;

    /// <summary>
    /// Nodes with fast changing values.
    /// </summary>
    public class FastPluginNodes : IPluginNodes
    {
        public TimeService TimeService { get; set; }
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();

        private uint NodeCount { get; } = 3;
        private uint NodeRate { get; } = 1000; // ms.
        private NodeType NodeType { get; } = NodeType.UIntScalar;
        private string NodeMinValue { get; }
        private string NodeMaxValue { get; }
        private string NodeStepSize { get; } = "1";
        private uint NodeSamplingInterval { get; } // ms.
        public ILogger Logger { get; set; }

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;
            _slowFastCommon = new SlowFastCommon(_plcNodeManager, TimeService, Logger);

#pragma warning disable CA2000 // Dispose objects before losing scope
            var folder = _plcNodeManager.CreateFolder(
                telemetryFolder,
                path: "Fast",
                name: "Fast",
                NamespaceType.PlcApplications);
#pragma warning restore CA2000 // Dispose objects before losing scope

            // Used for methods to limit the number of updates to a fixed count.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var simulatorFolder = _plcNodeManager.CreateFolder(
                telemetryFolder.Parent, // Root.
                path: "SimulatorConfiguration",
                name: "SimulatorConfiguration",
                NamespaceType.PlcApplications);
#pragma warning restore CA2000 // Dispose objects before losing scope

            AddNodes(folder, simulatorFolder);
            AddMethods(methodsFolder);
        }

        private void AddMethods(FolderState methodsFolder)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var stopUpdateMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: "StopUpdateFastNodes",
                name: "StopUpdateFastNodes",
                "Stop the increase of value of fast nodes",
                NamespaceType.PlcApplications);
#pragma warning restore CA2000 // Dispose objects before losing scope

            SetStopUpdateFastNodesProperties(ref stopUpdateMethod);

#pragma warning disable CA2000 // Dispose objects before losing scope
            var startUpdateMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: "StartUpdateFastNodes",
                name: "StartUpdateFastNodes",
                "Start the increase of value of fast nodes",
                NamespaceType.PlcApplications);
#pragma warning restore CA2000 // Dispose objects before losing scope

            SetStartUpdateFastNodesProperties(ref startUpdateMethod);
        }

        public void StartSimulation()
        {
            // Only use the fast timers when we need to go really fast,
            // since they consume more resources and create an own thread.
            _nodeGenerator = NodeRate >= 50 || !Stopwatch.IsHighResolution ?
                TimeService.NewTimer(UpdateNodes, NodeRate) :
                TimeService.NewFastTimer(UpdateVeryFastNodes, NodeRate);
        }

        public void StopSimulation()
        {
            if (_nodeGenerator != null)
            {
                _nodeGenerator.Enabled = false;
            }
        }

        private void AddNodes(FolderState folder, FolderState simulatorFolder)
        {
            (_nodes, _badNodes) = _slowFastCommon.CreateNodes(NodeType, "Fast",
                NodeCount, folder, simulatorFolder, false, NodeStepSize,
                NodeMinValue, NodeMaxValue, NodeRate, NodeSamplingInterval);

            ExposeNodesWithIntervals();
        }

        /// <summary>
        /// Expose node information for dumping pn.json.
        /// </summary>
        private void ExposeNodesWithIntervals()
        {
            var nodes = new List<NodeWithIntervals>();

            foreach (var node in _nodes)
            {
                nodes.Add(new NodeWithIntervals
                {
                    NodeId = node.NodeId.Identifier.ToString(),
                    Namespace = Plc.Namespaces.PlcApplications,
                    PublishingInterval = NodeRate,
                    SamplingInterval = NodeSamplingInterval,
                });
            }

            foreach (var node in _badNodes)
            {
                nodes.Add(new NodeWithIntervals
                {
                    NodeId = node.NodeId.Identifier.ToString(),
                    Namespace = Plc.Namespaces.PlcApplications,
                    PublishingInterval = NodeRate,
                    SamplingInterval = NodeSamplingInterval,
                });
            }

            Nodes = nodes;
        }

        private void SetStopUpdateFastNodesProperties(ref MethodState method)
        {
            method.OnCallMethod += OnStopUpdateFastNodes;
        }

        private void SetStartUpdateFastNodesProperties(ref MethodState method)
        {
            method.OnCallMethod += OnStartUpdateFastNodes;
        }

        /// <summary>
        /// Method to stop updating the fast nodes.
        /// </summary>
        private ServiceResult OnStopUpdateFastNodes(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _updateNodes = false;
            Logger.LogDebug("StopUpdateFastNodes method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to start updating the fast nodes.
        /// </summary>
        private ServiceResult OnStartUpdateFastNodes(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _updateNodes = true;
            Logger.LogDebug("StartUpdateFastNodes method called");
            return ServiceResult.Good;
        }

        private void UpdateNodes(object state, ElapsedEventArgs elapsedEventArgs)
        {
            _slowFastCommon.UpdateNodes(_nodes, _badNodes, NodeType, _updateNodes);
        }

        private void UpdateVeryFastNodes(object state, FastTimerElapsedEventArgs elapsedEventArgs)
        {
            _slowFastCommon.UpdateNodes(_nodes, _badNodes, NodeType, _updateNodes);
        }

        private PlcNodeManager _plcNodeManager;
        private SlowFastCommon _slowFastCommon;
        private BaseDataVariableState[] _nodes;
        private BaseDataVariableState[] _badNodes;
        private ITimer _nodeGenerator;
        private bool _updateNodes = true;
    }
}