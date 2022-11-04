// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Twin {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection(TwinTestCollection.CollectionName)]
    [Trait(TestConstants.TraitConstants.TwinModeTraitName, TestConstants.TraitConstants.DefaultTraitValue)]
    public class TwinBrowseTestTheory {
        private readonly TwinTestContext _context;

        public TwinBrowseTestTheory(TwinTestContext context, ITestOutputHelper output) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact, PriorityOrder(0)]
        public void BrowseAddressSpace() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = TestHelper.Twin.GetBrowseEndpointAsync(_context, _context.OpcUaEndpointId, null, cts.Token).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.Contains(nodes, n => string.Equals("i=85", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("i=86", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("i=87", n.NodeId));

            Assert.Equal(3, nodes.Count);
        }

        [Fact, PriorityOrder(1)]
        public void BrowseSpecificNode() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = TestHelper.Twin.GetBrowseEndpointAsync(_context, _context.OpcUaEndpointId, null, cts.Token).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            const string nodeId = "i=85";

            Assert.Contains(nodes, n => string.Equals(nodeId, n.NodeId));

            nodes = TestHelper.Twin.GetBrowseEndpointAsync(_context, _context.OpcUaEndpointId, nodeId).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.Contains(nodes, n => string.Equals("i=2253", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("http://microsoft.com/Opc/OpcPlc/Boiler#i=15070", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("http://microsoft.com/Opc/OpcPlc/#s=OpcPlc", n.NodeId));
        }

        [Fact, PriorityOrder(2)]
        public void BrowseSpecificNodeAbsolutePath() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            string nodeId = null; // Start browsing from Root
            var browsePath = new List<string> { "Objects", "Server", "ServerArray" };
            var result = TestHelper.Twin.GetBrowseNodePathAsync(_context, _context.OpcUaEndpointId, nodeId, browsePath, cts.Token).GetAwaiter().GetResult();

            var node = result.targets[0].target;
            Assert.Equal("ServerArray", node.browseName);
            Assert.Equal("i=2254", node.nodeId);
        }

        [Fact, PriorityOrder(3)]
        public void BrowseSpecificNodeRelativePath() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            string nodeId = "i=85"; // Start browsing from Objects
            var browsePath = new List<string> { "Server", "ServerArray" };
            var result = TestHelper.Twin.GetBrowseNodePathAsync(_context, _context.OpcUaEndpointId, nodeId, browsePath, cts.Token).GetAwaiter().GetResult();

            var node = result.targets[0].target;
            Assert.Equal("ServerArray", node.browseName);
            Assert.Equal("i=2254", node.nodeId);
        }

        [Fact, PriorityOrder(4)]
        public void BrowseAllNodesOfTypeObject() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = TestHelper.Twin.GetBrowseEndpointRecursiveAsync(_context, _context.OpcUaEndpointId, "Object", null, cts.Token).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.True(nodes.Count > 150);

            Assert.Contains(nodes, n => string.Equals("i=85", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("i=2253", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("http://microsoft.com/Opc/OpcPlc/Boiler#i=15070", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("http://microsoft.com/Opc/OpcPlc/#s=OpcPlc", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("i=86", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("i=87", n.NodeId));
        }

        [Fact, PriorityOrder(5)]
        public void BrowseAllNodesOfTypeVariable() {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = TestHelper.Twin.GetBrowseEndpointRecursiveAsync(_context, _context.OpcUaEndpointId, "Variable", null, cts.Token).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.True(nodes.Count > 2000);

            Assert.Contains(nodes, n => string.Equals("i=2254", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("i=11617", n.NodeId));
            Assert.Contains(nodes, n => string.Equals("http://microsoft.com/Opc/OpcPlc/#s=SlowUInt1", n.NodeId));
        }
    }
}
