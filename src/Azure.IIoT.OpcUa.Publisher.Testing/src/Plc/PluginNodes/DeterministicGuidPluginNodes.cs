// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Plc.PluginNodes
{
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Test;
    using Plc.PluginNodes.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Nodes with deterministic GUIDs as ID.
    /// </summary>
    public class DeterministicGuidPluginNodes : IPluginNodes
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();
        public TimeService TimeService { get; set; }
        public ILogger Logger { get; set; }
        public uint ScaleUnits { get; set; }
        private uint NodeCount => ScaleUnits == 0 ? 1u : ScaleUnits * 50;

        /// <summary>
        /// ms.
        /// </summary>
        private uint NodeRate { get; } = 1000;
        private NodeType NodeType { get; } = NodeType.UIntScalar;

        public void AddToAddressSpace(FolderState telemetryFolder, FolderState methodsFolder, PlcNodeManager plcNodeManager)
        {
            _plcNodeManager = plcNodeManager;
            var folder = _plcNodeManager.CreateFolder(
                telemetryFolder,
                path: "Deterministic GUID",
                name: "Deterministic GUID",
                NamespaceType.PlcApplications);
            AddNodes(folder);
        }

        public void StartSimulation()
        {
            foreach (var node in _nodes)
            {
                node.Start(value => value + 1, periodMs: 1000);
            }
        }

        public void StopSimulation()
        {
            foreach (var node in _nodes)
            {
                node.Stop();
            }
        }

        private void AddNodes(FolderState folder)
        {
            _nodes = new SimulatedVariableNode<uint>[NodeCount];
            var nodes = new List<NodeWithIntervals>((int)NodeCount);

            if (NodeCount > 0)
            {
                Logger.LogInformation("Creating {NodeCount} GUID node(s) of type: {NodeType}", NodeCount, NodeType);
                Logger.LogInformation("Node values will change every {NodeRate} ms", NodeRate);
            }

            for (var i = 0; i < NodeCount; i++)
            {
                var id = DeterministicGuid.NewGuid().ToString();

                _nodes[i] = _plcNodeManager.CreateVariableNode<uint>(
                    _plcNodeManager.CreateBaseVariable(
                        folder,
                        path: id,
                        name: id,
                        new NodeId((uint)BuiltInType.UInt32),
                        ValueRanks.Scalar,
                        AccessLevels.CurrentReadOrWrite,
                        "Constantly increasing value",
                        NamespaceType.PlcApplications,
                        defaultValue: (uint)0));

                nodes.Add(new NodeWithIntervals
                {
                    NodeId = id,
                    Namespace = Plc.Namespaces.PlcApplications
                });
            }

            Nodes = nodes;
        }
        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<uint>[] _nodes;
    }
}
