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

        private uint NodeCount { get; } = 3;
        private uint NodeRate { get; } = 10000; // ms.
        private NodeType NodeType { get; } = NodeType.UIntScalar;
        private string NodeMinValue { get; }
        private string NodeMaxValue { get; }
        private string NodeStepSize { get; } = "1";
        private uint NodeSamplingInterval { get; } // ms.

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

#pragma warning disable CA2000 // Dispose objects before losing scope
            var folder = _plcNodeManager.CreateFolder(
                telemetryFolder,
                path: "Slow",
                name: "Slow",
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
                path: "StopUpdateSlowNodes",
                name: "StopUpdateSlowNodes",
                "Stop the increase of value of slow nodes",
                NamespaceType.PlcApplications);
#pragma warning restore CA2000 // Dispose objects before losing scope

            SetStopUpdateSlowNodesProperties(ref stopUpdateMethod);

#pragma warning disable CA2000 // Dispose objects before losing scope
            var startUpdateMethod = _plcNodeManager.CreateMethod(
                methodsFolder,
                path: "StartUpdateSlowNodes",
                name: "StartUpdateSlowNodes",
                "Start the increase of value of slow nodes",
                NamespaceType.PlcApplications);
#pragma warning restore CA2000 // Dispose objects before losing scope

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
        private ServiceResult OnStopUpdateSlowNodes(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            _updateNodes = false;
            Logger.LogDebug("StopUpdateSlowNodes method called");
            return ServiceResult.Good;
        }

        /// <summary>
        /// Method to start updating the slow nodes.
        /// </summary>
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
