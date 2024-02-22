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
    using System.Timers;

    /// <summary>
    /// Nodes with slow changing values.
    /// </summary>
    public class SlowPluginNodes : IPluginNodes
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();
        public TimeService TimeService { get; set; }
        public ILogger Logger { get; set; }
        public uint ScaleUnits { get; set; }
        private uint NodeCount => ScaleUnits == 0 ? 3u : ScaleUnits * 1000;
        /// <summary>
        /// ms.
        /// </summary>
        private uint NodeRate { get; } = 10000;
        private NodeType NodeType { get; } = NodeType.UIntScalar;
        private string NodeMinValue { get; }
        private string NodeMaxValue { get; }
        private string NodeStepSize { get; } = "1";
        /// <summary>
        /// ms.
        /// </summary>
        private uint NodeSamplingInterval { get; }

        private PlcNodeManager _plcNodeManager;
        private SlowFastCommon _slowFastCommon;
        private BaseDataVariableState[] _nodes;
        private BaseDataVariableState[] _badNodes;
        private ITimer _nodeGenerator;
        private bool _updateNodes = true;

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;
            _slowFastCommon = new SlowFastCommon(_plcNodeManager, TimeService, Logger);
            var folder = _plcNodeManager.CreateFolder(
                telemetryFolder,
                path: "Slow",
                name: "Slow",
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
                path: "StopUpdateSlowNodes",
                name: "StopUpdateSlowNodes",
                "Stop the increase of value of slow nodes",
                NamespaceType.PlcApplications);
            SetStopUpdateSlowNodesProperties(ref stopUpdateMethod);
            var startUpdateMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: "StartUpdateSlowNodes",
                name: "StartUpdateSlowNodes",
                "Start the increase of value of slow nodes",
                NamespaceType.PlcApplications);
            SetStartUpdateSlowNodesProperties(ref startUpdateMethod);
        }

        public void StartSimulation()
        {
            _nodeGenerator = TimeService.NewTimer(UpdateNodes, NodeRate);
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
            (_nodes, _badNodes) = _slowFastCommon.CreateNodes(NodeType, "Slow", NodeCount,
                folder, simulatorFolder, false, NodeStepSize, NodeMinValue,
                NodeMaxValue, NodeRate, NodeSamplingInterval);

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

        private void SetStopUpdateSlowNodesProperties(ref MethodState method)
        {
            method.OnCallMethod += OnStopUpdateSlowNodes;
        }

        private void SetStartUpdateSlowNodesProperties(ref MethodState method)
        {
            method.OnCallMethod += OnStartUpdateSlowNodes;
        }

        /// <summary>
        /// Method to stop updating the slow nodes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        private ServiceResult OnStopUpdateSlowNodes(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _updateNodes = false;
            Logger.LogDebug("StopUpdateSlowNodes method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to start updating the slow nodes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="inputArguments"></param>
        /// <param name="outputArguments"></param>
        private ServiceResult OnStartUpdateSlowNodes(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _updateNodes = true;
            Logger.LogDebug("StartUpdateSlowNodes method called");
            return ServiceResult.Good;
        }

        private void UpdateNodes(object state, ElapsedEventArgs elapsedEventArgs)
        {
            _slowFastCommon.UpdateNodes(_nodes, _badNodes, NodeType, _updateNodes);
        }
    }
}
