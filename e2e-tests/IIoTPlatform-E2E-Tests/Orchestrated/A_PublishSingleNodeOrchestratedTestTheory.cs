// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Orchestrated
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Newtonsoft.Json;
    using RestSharp;
    using TestExtensions;
    using Xunit.Abstractions;
    using System.Threading;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// The test theory using different (ordered) test cases to go thru all required steps of publishing OPC UA node
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Collection("IIoT Multiple Nodes Test Collection")]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeOrchestratedTraitValue)]
    public class APublishSingleNodeOrchestratedTestTheory
    {
        private readonly ITestOutputHelper _output;
        private readonly IIoTMultipleNodesTestContext _context;

        public APublishSingleNodeOrchestratedTestTheory(IIoTMultipleNodesTestContext context, ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
        }

        [Fact, PriorityOrder(0)]
        public async Task TestSetUnmanagedTagFalse()
        {
            _context.Reset();
            await TestHelper.SwitchToOrchestratedModeAsync(_context).ConfigureAwait(false);
        }

        [Fact, PriorityOrder(1)]
        public async Task TestCollectOAuthToken()
        {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var token = await TestHelper.GetTokenAsync(_context, cts.Token).ConfigureAwait(false);
            Assert.NotEmpty(token);
        }

        [Fact, PriorityOrder(2)]
        public async Task TestReadSimulatedOpcUaNodes()
        {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var simulatedOpcServer = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token).ConfigureAwait(false);
            Assert.NotNull(simulatedOpcServer);
            Assert.NotEmpty(simulatedOpcServer.Keys);
            Assert.NotEmpty(simulatedOpcServer.Values);
        }

        [Fact, PriorityOrder(3)]
        public async Task TestRegisterOPCServerExpectSuccess()
        {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // We will wait for microservices of IIoT platform to be healthy and modules to be deployed.
            await TestHelper.WaitForServicesAsync(_context, cts.Token).ConfigureAwait(false);
            await _context.RegistryHelper.WaitForIIoTModulesConnectedAsync(_context.DeviceConfig.DeviceId, cts.Token).ConfigureAwait(false);

            var endpointUrl = TestHelper.GetSimulatedOpcServerUrls(_context).First();
            _context.OpcServerUrl = endpointUrl;

            var body = new
            {
                discoveryUrl = endpointUrl
            };

            const string route = TestConstants.APIRoutes.RegistryApplications;
            var response = TestHelper.CallRestApi(_context, Method.Post, route, body, ct: cts.Token);
            Assert.True(response.IsSuccessful, $"Got {response.StatusCode} registering {endpointUrl} discovery url");
        }

        [Fact, PriorityOrder(4)]
        public async Task TestGetApplicationsFromRegistryExpectOneRegisteredApplication()
        {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            dynamic json = await TestHelper.Discovery.WaitForDiscoveryToBeCompletedAsync(
                _context, new HashSet<string> { _context.OpcServerUrl }, cts.Token).ConfigureAwait(false);
            Assert.True(json != null, $"OPC Application with url {_context.OpcServerUrl} not found");
        }

        [Fact, PriorityOrder(5)]
        public async Task TestGetEndpointsExpectOneWithMultipleAuthentication()
        {
            // used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcServerUrl))
            {
                await TestRegisterOPCServerExpectSuccess().ConfigureAwait(false);
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcServerUrl));
            }

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var json = await TestHelper.Discovery.WaitForEndpointDiscoveryToBeCompletedAsync(
                _context, new HashSet<string> { _context.OpcServerUrl }, "SignAndEncrypt",
                "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256", cts.Token).ConfigureAwait(false);
            Assert.NotNull(json);

            var opcServerEndpoints = ((IEnumerable<dynamic>)json.items)
                .Where(item => item.registration.endpoint.url.Trim('/') == _context.OpcServerUrl.Trim('/')
                    && item.registration.endpoint.securityMode == "SignAndEncrypt"
                    && item.registration.endpoint.securityPolicy == "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256");

            Assert.Single(opcServerEndpoints);

            var result = opcServerEndpoints.FirstOrDefault().registration;
            var endpointId = (string)result.id;
            Assert.NotEmpty(endpointId);

            // Authentication Checks
            Assert.Equal("None", result.authenticationMethods[0].credentialType);
            Assert.Equal("UserName", result.authenticationMethods[1].credentialType);
            Assert.Equal("X509Certificate", result.authenticationMethods[2].credentialType);

            // Store id of endpoint for further interaction
            _context.OpcUaEndpointId = endpointId;
        }

        [Fact, PriorityOrder(6)]
        public async Task TestActivateEndpointExpectSuccess()
        {
            // Used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId))
            {
                await TestGetEndpointsExpectOneWithMultipleAuthentication().ConfigureAwait(false);
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            await TestHelper.Registry.ActivateEndpointAsync(_context, _context.OpcUaEndpointId).ConfigureAwait(false);
        }

        [Fact, PriorityOrder(7)]
        public async Task TestCheckIfEndpointWasActivatedExpectActivatedAndConnected()
        {
            Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var json = await TestHelper.Registry.WaitForEndpointToBeActivatedAsync(_context, new HashSet<string> { _context.OpcUaEndpointId }, cts.Token).ConfigureAwait(false);
            Assert.True(json != null, "OPC UA Endpoint not found");
        }

        [Fact, PriorityOrder(8)]
        public async Task TestPublishNodeWithDefaultsExpectDataAvailableAtIoTHub()
        {
            // used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId))
            {
                await TestGetEndpointsExpectOneWithMultipleAuthentication().ConfigureAwait(false);
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var simulatedOpcServer = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token).ConfigureAwait(false);

            var route = string.Format(CultureInfo.InvariantCulture, TestConstants.APIRoutes.PublisherStartFormat, _context.OpcUaEndpointId);
            var body = new
            {
                item = new
                {
                    nodeId = simulatedOpcServer.Values.First().OpcNodes
                    .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                    .Skip(2).First().Id,
                    samplingInterval = "00:00:00.250",
                    publishingInterval = "00:00:00.500",
                }
            };

            var response = TestHelper.CallRestApi(_context, Method.Post, route, body, ct: cts.Token);
            Assert.True(response.IsSuccessful, $"Got {response.StatusCode} starting publishing");
            Assert.Equal("{}", response.Content);
        }

        [Fact, PriorityOrder(9)]
        public async Task TestGetListOfJobsExpectOneJobWithPublishingOneNode()
        {
            // Used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId))
            {
                await TestGetEndpointsExpectOneWithMultipleAuthentication().ConfigureAwait(false);
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var simulatedOpcServer = await TestHelper.GetSimulatedPublishedNodesConfigurationAsync(_context, cts.Token).ConfigureAwait(false);
            var route = TestConstants.APIRoutes.PublisherJobs;
            var response = TestHelper.CallRestApi(_context, Method.Get, route, ct: cts.Token);
            Assert.True(response.IsSuccessful, $"Got {response.StatusCode} Getting publishing jobs");
            dynamic json = JsonConvert.DeserializeObject(response.Content);

            Assert.NotEqual(0, json.jobs.Count);

            var jobConfiguration = json.jobs[0].jobConfiguration;
            Assert.NotNull(jobConfiguration);
            Assert.NotNull(jobConfiguration.writerGroup);
            Assert.NotNull(jobConfiguration.writerGroup.dataSetWriters);
            Assert.Equal(1, (int)jobConfiguration.writerGroup.dataSetWriters.Count);

            var dataSet = jobConfiguration.writerGroup.dataSetWriters[0].dataSet;
            Assert.NotNull(dataSet);
            Assert.NotNull(dataSet.dataSetSource);
            Assert.NotNull(dataSet.dataSetSource.publishedVariables.publishedData);
            Assert.Equal(1, (int)dataSet.dataSetSource.publishedVariables.publishedData.Count);
            Assert.NotEmpty((string)dataSet.dataSetSource.publishedVariables.publishedData[0].publishedVariableNodeId);
            var publishedNodeId = (string)dataSet.dataSetSource.publishedVariables.publishedData[0].publishedVariableNodeId;
            Assert.Equal(simulatedOpcServer.Values.First().OpcNodes
                .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                .Skip(2).First().Id, publishedNodeId);
        }

        [Fact, PriorityOrder(10)]
        public async Task TestVerifyDataAvailableAtIoTHub()
        {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Make sure that there is no active monitoring.
            await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token).ConfigureAwait(false);

            // Wait some time to generate events to process
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds * 4, cts.Token).ConfigureAwait(false);
            var json = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);
            Assert.True(json.TotalValueChangesCount > 0, "No messages received at IoT Hub");
            Assert.True(json.DroppedValueCount == 0, "Dropped messages detected");
            Assert.True(json.DuplicateValueCount == 0, "Duplicate values detected");
            Assert.Equal(0U, json.DroppedSequenceCount);
            // Uncomment once bug generating duplicate sequence numbers is resolved.
            //Assert.Equal(0U, json.DuplicateSequenceCount);
            Assert.Equal(0U, json.ResetSequenceCount);
        }

        [Fact, PriorityOrder(11)]
        public async Task RemoveJobExpectSuccess()
        {
            // Used if running test cases separately (during development)
            if (string.IsNullOrWhiteSpace(_context.OpcUaEndpointId))
            {
                await TestGetEndpointsExpectOneWithMultipleAuthentication().ConfigureAwait(false);
                Assert.False(string.IsNullOrWhiteSpace(_context.OpcUaEndpointId));
            }

            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            var route = string.Format(CultureInfo.InvariantCulture, TestConstants.APIRoutes.PublisherJobsFormat, _context.OpcUaEndpointId);
            var response = TestHelper.CallRestApi(_context, Method.Delete, route, ct: cts.Token);
            Assert.True(response.IsSuccessful, $"Got {response.StatusCode} deleting publishing job");
        }

        [Fact, PriorityOrder(12)]
        public async Task TestVerifyNoDataIncomingAtIoTHub()
        {
            var cts = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);

            // Wait untill the publishing has stopped
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds * 4, cts.Token).ConfigureAwait(false);

            // Make sure that there is no active monitoring.
            await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);

            // Use test event processor to verify data send to IoT Hub (expected* set to zero as data gap analysis is not part of this test case)
            await TestHelper.StartMonitoringIncomingMessagesAsync(_context, 0, 0, 0, cts.Token).ConfigureAwait(false);

            // Wait some time to generate events to process
            await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, cts.Token).ConfigureAwait(false);
            var json = await TestHelper.StopMonitoringIncomingMessagesAsync(_context, cts.Token).ConfigureAwait(false);
            Assert.True(json.TotalValueChangesCount == 0, $"{json.TotalValueChangesCount} Messages received at IoT Hub");
        }
    }
}
