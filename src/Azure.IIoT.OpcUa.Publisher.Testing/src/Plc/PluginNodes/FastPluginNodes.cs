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

        private uint NodeCount => ScaleUnits == 0 ? 3u : ScaleUnits * 1000;
        public uint ScaleUnits { get; set; }
        /// <summary>
        /// ms.
        /// </summary>
        private uint NodeRate { get; } = 1000;
        private NodeType NodeType { get; } = NodeType.UIntScalar;
        private string NodeMinValue { get; }
        private string NodeMaxValue { get; }
        private string NodeStepSize { get; } = "1";
        /// <summary>
        /// ms.
        /// </summary>
        private uint NodeSamplingInterval { get; }
        public ILogger Logger { get; set; }

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;
            _slowFastCommon = new SlowFastCommon(_plcNodeManager, TimeService, Logger);
            var folder = _plcNodeManager.CreateFolder(
                telemetryFolder,
                path: "Fast",
                name: "Fast",
                NamespaceType.PlcApplications);

            // Used for methods to limit the number of updates to a fixed count.
            var simulatorFolder = _plcNodeManager.CreateFolder(
                telemetryFolder.Parent, // Root.
                path: "SimulatorConfiguration",
                name: "SimulatorConfiguration",
                NamespaceType.PlcApplications);
            AddNodes(folder, simulatorFolder);
            AddMethods(methodsFolder);
        }

        private void AddMethods(FolderState methodsFolder)
        {
            var stopUpdateMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: "StopUpdateFastNodes",
                name: "StopUpdateFastNodes",
                "Stop the increase of value of fast nodes",
                NamespaceType.PlcApplications);
            SetStopUpdateFastNodesProperties(ref stopUpdateMethod);
            var startUpdateMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: "StartUpdateFastNodes",
                name: "StartUpdateFastNodes",
                "Start the increase of value of fast nodes",
                NamespaceType.PlcApplications);
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
                    NodeIdTypePrefix = NodeWithIntervals.GetPrefix(node.NodeId.IdType),
                    Namespace = Plc.Namespaces.PlcApplications,
                    PublishingInterval = NodeRate,
                    SamplingInterval = NodeSamplingInterval
                });
            }

            foreach (var node in _badNodes)
            {
                nodes.Add(new NodeWithIntervals
                {
                    NodeId = node.NodeId.Identifier.ToString(),
                    NodeIdTypePrefix = NodeWithIntervals.GetPrefix(node.NodeId.IdType),
                    Namespace = Plc.Namespaces.PlcApplications,
                    PublishingInterval = NodeRate,
                    SamplingInterval = NodeSamplingInterval
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
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        private ServiceResult OnStopUpdateFastNodes(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _updateNodes = false;
            Logger.LogDebug("StopUpdateFastNodes method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to start updating the fast nodes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
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
