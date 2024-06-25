// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests.Twin
{
    using IIoTPlatformE2ETests.TestExtensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection(TwinTestCollection.CollectionName)]
    [Trait(TestConstants.TraitConstants.TwinModeTraitName, TestConstants.TraitConstants.DefaultTraitValue)]
    public class TwinBrowseTestTheory
    {
        private readonly TwinTestContext _context;

        public TwinBrowseTestTheory(TwinTestContext context, ITestOutputHelper output)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.SetOutputHelper(output);
        }

        [Fact, PriorityOrder(0)]
        public async Task TestPrepareAsync()
        {
            await _context.AssertTestEnvironmentPreparedAsync();
        }

        [Fact, PriorityOrder(1)]
        public async Task BrowseAddressSpace()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = await TestHelper.Twin.GetBrowseEndpointAsync(_context, _context.OpcUaEndpointId, null, cts.Token);

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.Contains(nodes, n => string.Equals("i=85", n.NodeId, StringComparison.Ordinal));
            Assert.Contains(nodes, n => string.Equals("i=86", n.NodeId, StringComparison.Ordinal));
            Assert.Contains(nodes, n => string.Equals("i=87", n.NodeId, StringComparison.Ordinal));

            Assert.Equal(3, nodes.Count);
        }

        [Fact, PriorityOrder(2)]
        public async Task BrowseSpecificNode()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = await TestHelper.Twin.GetBrowseEndpointAsync(_context, _context.OpcUaEndpointId, null, cts.Token);

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            const string nodeId = "i=85";

            Assert.Contains(nodes, n => string.Equals(nodeId, n.NodeId, StringComparison.Ordinal));

            nodes = await TestHelper.Twin.GetBrowseEndpointAsync(_context, _context.OpcUaEndpointId, nodeId);

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.Contains(nodes, n => string.Equals("i=2253", n.NodeId, StringComparison.Ordinal));
            Assert.Contains(nodes, n => string.Equals("nsu=http://microsoft.com/Opc/OpcPlc/Boiler;i=5", n.NodeId, StringComparison.Ordinal));
            Assert.Contains(nodes, n => string.Equals("nsu=http://microsoft.com/Opc/OpcPlc/;s=OpcPlc", n.NodeId, StringComparison.Ordinal));
        }

        [Fact, PriorityOrder(3)]
        public async Task BrowseSpecificNodeAbsolutePath()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            const string nodeId = null; // Start browsing from Root
            var browsePath = new List<string> { "Objects", "Server", "ServerArray" };
            var result = await TestHelper.Twin.GetBrowseNodePathAsync(_context, _context.OpcUaEndpointId, nodeId, browsePath, cts.Token);

            var node = result.targets[0].target;
            Assert.Equal("ServerArray", node.browseName);
            Assert.Equal("i=2254", node.nodeId);
        }

        [Fact, PriorityOrder(4)]
        public async Task BrowseSpecificNodeRelativePath()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            const string nodeId = "i=85"; // Start browsing from Objects
            var browsePath = new List<string> { "Server", "ServerArray" };
            var result = await TestHelper.Twin.GetBrowseNodePathAsync(_context, _context.OpcUaEndpointId, nodeId, browsePath, cts.Token);

            var node = result.targets[0].target;
            Assert.Equal("ServerArray", node.browseName);
            Assert.Equal("i=2254", node.nodeId);
        }

        [Fact, PriorityOrder(5)]
        public async Task BrowseAllNodesOfTypeObject()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = await TestHelper.Twin.GetBrowseEndpointRecursiveAsync(_context, _context.OpcUaEndpointId, -1, "Object", "i=85", cts.Token);

            try
            {
                _context.OutputHelper.WriteLine($"Node count is {nodes.Count}");

                Assert.NotNull(nodes);
                Assert.NotEmpty(nodes);

                Assert.True(nodes.Count > 150);

                var set = new HashSet<string>
                {
                    "i=2253",
                    "nsu=http://microsoft.com/Opc/OpcPlc/Boiler;i=5",
                    "nsu=http://microsoft.com/Opc/OpcPlc/;s=OpcPlc",
                    "i=15668",
                    "nsu=http://microsoft.com/Opc/OpcPlc/ReferenceTest;s=Scalar_Static_Mass_Boolean"
                };
                var counted = nodes.Count(n => set.Contains(n.NodeId));
                _context.OutputHelper.WriteLine($"Found count is {counted}");
                Assert.Equal(set.Count, counted);
            }
            catch
            {
                nodes.ForEach(n => _context.OutputHelper.WriteLine(n.NodeId));
                throw;
            }
        }

        [Fact, PriorityOrder(6)]
        public async Task BrowseAllNodesOfTypeVariable()
        {
            using var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            var nodes = await TestHelper.Twin.GetBrowseEndpointRecursiveAsync(_context, _context.OpcUaEndpointId, -1, "Variable", "i=85", cts.Token);

            try
            {
                _context.OutputHelper.WriteLine($"Node count is {nodes.Count}");

                Assert.NotNull(nodes);
                Assert.NotEmpty(nodes);

                Assert.True(nodes.Count > 5000);

                var set = new HashSet<string>
                {
                    "i=2254",
                    "nsu=http://microsoft.com/Opc/OpcPlc/;s=LongString10kB",
                    "nsu=http://microsoft.com/Opc/OpcPlc/;s=SlowUInt1",
                    "nsu=http://microsoft.com/Opc/OpcPlc/ReferenceTest;s=Scalar_Simulation_Mass_Boolean_Boolean_22"
                };
                var counted = nodes.Count(n => set.Contains(n.NodeId));
                _context.OutputHelper.WriteLine($"Found count is {counted}");
                Assert.Equal(set.Count, counted);
            }
            catch
            {
                nodes.ForEach(n => _context.OutputHelper.WriteLine(n.NodeId));
                throw;
            }
        }
    }
}
