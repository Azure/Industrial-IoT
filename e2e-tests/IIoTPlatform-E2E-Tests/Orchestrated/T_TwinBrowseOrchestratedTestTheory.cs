// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Orchestrated {
    using IIoTPlatform_E2E_Tests.TestExtensions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using RestSharp;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Multiple Nodes Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeOrchestratedTraitValue)]
    public class T_TwinBrowseOrchestratedTestTheory {
        private readonly IIoTMultipleNodesTestContext _context;

        public T_TwinBrowseOrchestratedTestTheory(IIoTMultipleNodesTestContext context, ITestOutputHelper output) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact, PriorityOrder(0)]
        public void SwitchToOrchestratedMode() {
            TestHelper.SwitchToOrchestratedModeAsync(_context).GetAwaiter().GetResult();
        }

        [Fact, PriorityOrder(1)]
        public void D1_Discover_OPC_UA_Endpoints() {
            var endpoints = TestHelper.Registry_GetEndpoints(_context).GetAwaiter().GetResult();

            Assert.NotEmpty(endpoints);

            var endpoint = endpoints.First();

            Assert.NotNull(endpoint.Id);
            Assert.Equal("ActivatedAndConnected", endpoint.ActivationState);
            Assert.Equal("Ready", endpoint.EndpointState);
        }

        [Fact, PriorityOrder(2)]
        public void T1_Browse_Address_Space() {
            var endpoints = TestHelper.Registry_GetEndpoints(_context).GetAwaiter().GetResult();

            Assert.NotEmpty(endpoints);

            var endpoint = endpoints.First();

            Assert.NotNull(endpoint.Id);
            Assert.Equal("ActivatedAndConnected", endpoint.ActivationState);
            Assert.Equal("Ready", endpoint.EndpointState);

            var nodes = TestHelper.Twin_GetBrowseEndpoint(_context, endpoint.Id).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);
        }

        [Fact, PriorityOrder(3)]
        public void T1_1_BrowseSpecificNode() {
            var endpoints = TestHelper.Registry_GetEndpoints(_context).GetAwaiter().GetResult();

            Assert.NotEmpty(endpoints);

            var endpoint = endpoints.First();

            Assert.NotNull(endpoint.Id);
            Assert.Equal("ActivatedAndConnected", endpoint.ActivationState);
            Assert.Equal("Ready", endpoint.EndpointState);

            var nodes = TestHelper.Twin_GetBrowseEndpoint(_context, endpoint.Id).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            var nodeId = nodes.First().NodeId;

            nodes = TestHelper.Twin_GetBrowseEndpoint(_context, endpoint.Id, nodeId).GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);
        }

        [Fact, PriorityOrder(4)]
        public void T1_2_BrowseAllNodesOfTypeObject() {
            var endpoints = TestHelper.Registry_GetEndpoints(_context).GetAwaiter().GetResult();

            Assert.NotEmpty(endpoints);

            var endpoint = endpoints.First();

            Assert.NotNull(endpoint.Id);
            Assert.Equal("ActivatedAndConnected", endpoint.ActivationState);
            Assert.Equal("Ready", endpoint.EndpointState);

            var nodes = TestHelper.Twin_GetBrowseEndpoint_Recursive(_context, endpoint.Id, "Object").GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.True(nodes.Count > 150);
        }

        [Fact, PriorityOrder(5)]
        public void T1_3_BrowseAllNodesOfTypeVariable() {
            var endpoints = TestHelper.Registry_GetEndpoints(_context).GetAwaiter().GetResult();

            Assert.NotEmpty(endpoints);

            var endpoint = endpoints.First();

            Assert.NotNull(endpoint.Id);
            Assert.Equal("ActivatedAndConnected", endpoint.ActivationState);
            Assert.Equal("Ready", endpoint.EndpointState);

            var nodes = TestHelper.Twin_GetBrowseEndpoint_Recursive(_context, endpoint.Id, "Variable").GetAwaiter().GetResult();

            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);

            Assert.True(nodes.Count > 150);
        }
    }
}
