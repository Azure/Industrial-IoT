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
    using System.Text;

    /// <summary>
    /// Nodes that change value every second to string containing single repeated uppercase letter.
    /// </summary>
    public class LongStringPluginNodes : IPluginNodes
    {
        public IReadOnlyCollection<NodeWithIntervals> Nodes { get; private set; } = new List<NodeWithIntervals>();
        public TimeService TimeService { get; set; }
        public ILogger Logger { get; set; }
        public uint ScaleUnits { get; set; }

        private PlcNodeManager _plcNodeManager;
        private SimulatedVariableNode<string> _longStringIdNode10;
        private SimulatedVariableNode<string> _longStringIdNode50;
        private SimulatedVariableNode<byte[]> _longStringIdNode100;
        private SimulatedVariableNode<byte[]> _longStringIdNode200;
        private readonly Random _random = new();
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
            // Change value every second to string containing single repeated uppercase letter.
            const int A = 65, Z = 90 + 1;

#pragma warning disable CA5394 // Do not use insecure randomness
            _longStringIdNode10.Start(_ => new string((char)_random.Next(A, Z), 10 * 1024), periodMs: 1000);
#pragma warning restore CA5394 // Do not use insecure randomness
#pragma warning disable CA5394 // Do not use insecure randomness
            _longStringIdNode50.Start(_ => new string((char)_random.Next(A, Z), 50 * 1024), periodMs: 1000);
#pragma warning restore CA5394 // Do not use insecure randomness
#pragma warning disable CA5394 // Do not use insecure randomness
            _longStringIdNode100.Start(_ => Encoding.UTF8.GetBytes(new string((char)_random.Next(A, Z), 100 * 1024)), periodMs: 1000);
#pragma warning restore CA5394 // Do not use insecure randomness
#pragma warning disable CA5394 // Do not use insecure randomness
            _longStringIdNode200.Start(_ => Encoding.UTF8.GetBytes(new string((char)_random.Next(A, Z), 200 * 1024)), periodMs: 1000);
#pragma warning restore CA5394 // Do not use insecure randomness
        }

        public void StopSimulation()
        {
            _longStringIdNode10.Stop();
            _longStringIdNode50.Stop();
            _longStringIdNode100.Stop();
            _longStringIdNode200.Stop();
        }

        private void AddNodes(FolderState folder)
        {
            // 10 kB.
            var initialString = new string('A', 10 * 1024);
            _longStringIdNode10 = _plcNodeManager.CreateVariableNode<string>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "LongString10kB",
                    name: "LongString10kB",
                    new NodeId((uint)BuiltInType.String),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentReadOrWrite,
                    "Long string",
                    NamespaceType.PlcApplications,
                    initialString));

            // 50 kB.
            initialString = new string('A', 50 * 1024);
            _longStringIdNode50 = _plcNodeManager.CreateVariableNode<string>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "LongString50kB",
                    name: "LongString50kB",
                    new NodeId((uint)BuiltInType.String),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentReadOrWrite,
                    "Long string",
                    NamespaceType.PlcApplications,
                    initialString));

            // 100 kB.
            var initialByteArray = Encoding.UTF8.GetBytes(new string('A', 100 * 1024));
            _longStringIdNode100 = _plcNodeManager.CreateVariableNode<byte[]>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "LongString100kB",
                    name: "LongString100kB",
                    new NodeId((uint)BuiltInType.ByteString),
                    ValueRanks.Scalar,
                    AccessLevels.CurrentReadOrWrite,
                    "Long string",
                    NamespaceType.PlcApplications,
                    initialByteArray));

            // 200 kB.
            initialByteArray = Encoding.UTF8.GetBytes(new string('A', 200 * 1024));
            _longStringIdNode200 = _plcNodeManager.CreateVariableNode<byte[]>(
                _plcNodeManager.CreateBaseVariable(
                    folder,
                    path: "LongString200kB",
                    name: "LongString200kB",
                    new NodeId((uint)BuiltInType.Byte),
                    ValueRanks.OneDimension,
                    AccessLevels.CurrentReadOrWrite,
                    "Long string",
                    NamespaceType.PlcApplications,
                    initialByteArray));

            Nodes = new List<NodeWithIntervals>
            {
                new() {
                    NodeId = "LongString10kB",
                    Namespace = Plc.Namespaces.PlcApplications
                },
                new() {
                    NodeId = "LongString50kB",
                    Namespace = Plc.Namespaces.PlcApplications
                },
                new() {
                    NodeId = "LongString100kB",
                    Namespace = Plc.Namespaces.PlcApplications
                },
                new() {
                    NodeId = "LongString200kB",
                    Namespace = Plc.Namespaces.PlcApplications
                }
            };
        }
    }
}
