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
    using System.Text;

    /// <summary>
    /// Node with ID of 3950 chars.
    /// </summary>
    public class LongIdPluginNode : IPluginNodes
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
                path: "Special",
                name: "Special",
                NamespaceType.PlcApplications);
            AddNodes(folder);
        }

        public void StartSimulation()
        {
            _node.Start(value => value + 1, periodMs: 1000);
        }

        public void StopSimulation()
        {
            _node.Stop();
        }

        private void AddNodes(FolderState folder)
        {
            // Repeat A-Z until 3950 chars are collected.
            var id = new StringBuilder(4000);
            for (var i = 0; i < 3950; i++)
            {
                id.Append((char)(65 + (i % 26)));
            }

            _node = _plcNodeManager.CreateVariableNode<uint>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: id.ToString(),
                    name: "LongId3950",
                    new NodeId((uint)BuiltInType.UInt32),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentReadOrWrite,
                    "Constantly increasing value",
                    NamespaceType.PlcApplications,
                    defaultValue: (uint)0));

            Nodes = new List<NodeWithIntervals>
            {
                new() {
                    NodeId = id.ToString(),
                    Namespace = Plc.Namespaces.PlcApplications
                }
            };
        }

        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<uint> _node;
    }
}
