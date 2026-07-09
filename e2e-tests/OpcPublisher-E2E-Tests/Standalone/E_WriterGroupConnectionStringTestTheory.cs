// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.Standalone
{
    using OpcPublisherAEE2ETests.Deploy;
    using OpcPublisherAEE2ETests.TestExtensions;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.Messaging.EventHubs.Consumer;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Azure.Devices;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Verifies that a writer group configured with an IoT Hub device connection string
    /// (via WriterGroupTransportConfiguration) publishes telemetry to IoT Hub under that
    /// dedicated child device identity rather than the edge/module identity. This enables
    /// spreading a single edge device's telemetry across multiple IoT Hub partitions
    /// (see issue #2426).
    ///
    /// The test deploys a second publisher module with a distinct module identity, its own
    /// published nodes configuration file and pki folder, so it does not conflict with the
    /// default standalone publisher used by the other tests.
    /// </summary>
    [TestCaseOrderer(TestCaseOrderer.FullName, TestConstants.TestAssemblyName)]
    [Trait(TestConstants.TraitConstants.PublisherModeTraitName, TestConstants.TraitConstants.PublisherModeTraitValue)]
    public sealed class EWriterGroupConnectionStringTestTheory : IClassFixture<IIoTStandaloneTestContext>, IDisposable
    {
        private readonly IIoTStandaloneTestContext _context;
        private readonly CancellationToken _timeoutToken;
        private readonly EventHubConsumerClient _consumer;
        private readonly IoTHubPublisherDeployment _deployment;
        private readonly ServiceClient _iotHubClient;
        private readonly ISerializer _serializer;
        private readonly CancellationTokenSource _timeoutTokenSource;
        private readonly string _writerId;
        private readonly string _writerGroup;
        private readonly string _childDeviceId;

        public EWriterGroupConnectionStringTestTheory(IIoTStandaloneTestContext context, ITestOutputHelper output)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.SetOutputHelper(output);
            _timeoutTokenSource = new CancellationTokenSource(TestConstants.MaxTestTimeoutMilliseconds);
            _timeoutToken = _timeoutTokenSource.Token;
            _consumer = _context.GetEventHubConsumerClient();
            _serializer = new NewtonsoftJsonSerializer();
            _writerId = Guid.NewGuid().ToString();
            _writerGroup = Guid.NewGuid().ToString();
            _childDeviceId = "e2e-writergroup-cs-" + _context.TestingSuffix;

            // Second publisher identity with dedicated published nodes file and pki folder.
            _deployment = new IoTHubPublisherDeployment(_context, OpcPublisherAEE2ETests.MessagingMode.PubSub,
                moduleName: kModuleName,
                deploymentName: kDeploymentName,
                publishedNodesFile: TestConstants.PublishedNodesFolder + "/published_nodes_cs.json",
                pkiPath: TestConstants.PublishedNodesFolder + "/pki_cs");

            _iotHubClient = TestHelper.DeviceServiceClient(
                _context.IoTHubConfig.IoTHubConnectionString,
                Microsoft.Azure.Devices.TransportType.Amqp_WebSocket_Only);
        }

        public void Dispose()
        {
            _consumer?.CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
            _consumer?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _iotHubClient?.Dispose();
            _timeoutTokenSource?.Dispose();
        }

        [Fact, PriorityOrder(0)]
        public async Task TestDeploySecondPublisher()
        {
            await _context.RegistryHelper.DeployStandalonePublisherAsync(_deployment, _timeoutToken);

            // Reaching IoT Hub "Connected" state does not guarantee the freshly deployed
            // module is ready to serve direct methods: its method handlers and configuration
            // services may still be initializing, during which an invocation transiently
            // fails (405 while the handler registers, 5xx while the services start). Wait
            // until a benign read-only method succeeds before any test relies on it.
            await WaitUntilPublisherReadyAsync(_timeoutToken);
        }

        [Fact, PriorityOrder(1)]
        public async Task TestWriterGroupPublishesUnderChildDeviceIdentity()
        {
            // Arrange - start an OPC PLC simulation.
            await TestHelper.CreateSimulationContainerAsync(_context, new List<string>
                {"/bin/sh", "-c", "./opcplc --autoaccept --pn=50000"},
                _timeoutToken);

            // Register the child device and obtain a device connection string for it.
            var connectionString = await _context.RegistryHelper
                .GetOrCreateChildDeviceConnectionStringAsync(_childDeviceId, _timeoutToken);

            // Act - publish a fast changing node under a writer group bound to the child device.
            var pnJson = _context.PublishedNodesWithConnectionStringJson(
                50000,
                _writerId,
                _writerGroup,
                connectionString,
                new JArray(
                    new JObject(
                        new JProperty("Id", "ns=0;i=2258"), // Server CurrentTime, changes every second
                        new JProperty("OpcSamplingInterval", 1000),
                        new JProperty("OpcPublishingInterval", 1000))));
            await PublishNodesAsync(pnJson, _timeoutToken);

            // Assert - telemetry reaches IoT Hub attributed to the child device identity.
            var deviceId = await _consumer.ReadConnectionDeviceIdForWriterIdAsync(
                _writerId, _context, _timeoutToken);
            Assert.Equal(_childDeviceId, deviceId);

            await UnpublishAllNodesAsync(_timeoutToken);
        }

        [Fact, PriorityOrder(998)]
        public async Task TestCleanup()
        {
            await TestHelper.DeleteSimulationContainerAsync(_context, _timeoutToken);
            await _deployment.DeleteLayeredDeploymentAsync(_timeoutToken);
            await _context.RegistryHelper.RemoveDeviceAsync(_childDeviceId, _timeoutToken);
        }

        private async Task<MethodResultModel> CallMethodAsync(MethodParameterModel parameters, CancellationToken ct)
        {
            return await TestHelper.CallMethodAsync(
                _iotHubClient,
                _context.DeviceConfig.DeviceId,
                _deployment.ModuleName,
                parameters,
                _context,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Wait until the freshly deployed publisher module is ready to serve direct
        /// methods by polling a benign, read-only method until it returns 200. Transient
        /// 405 (handler not yet registered) and 5xx (services still starting) responses
        /// are tolerated; the loop is bounded by the test timeout token.
        /// </summary>
        private async Task WaitUntilPublisherReadyAsync(CancellationToken ct)
        {
            while (true)
            {
                var result = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                    },
                    ct).ConfigureAwait(false);

                if (result.Status == (int)HttpStatusCode.OK)
                {
                    return;
                }

                _context.OutputHelper?.WriteLine(
                    $"Publisher {_deployment.ModuleName} not ready yet (status {result.Status}), retrying...");
                await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct).ConfigureAwait(false);
            }
        }

        private async Task PublishNodesAsync(string json, CancellationToken ct)
        {
            await UnpublishAllNodesAsync(ct).ConfigureAwait(false);
            var entries = _serializer.Deserialize<PublishedNodesEntryModel[]>(json);
            foreach (var entry in entries)
            {
                var result = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.PublishNodes,
                        JsonPayload = _serializer.SerializeToString(entry)
                    },
                    ct).ConfigureAwait(false);

                Assert.Equal((int)HttpStatusCode.OK, result.Status);
            }

            var result1 = await CallMethodAsync(
                new MethodParameterModel
                {
                    Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                },
                ct).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, result1.Status);
            var response = _serializer.Deserialize<GetConfiguredEndpointsResponseModel>(result1.JsonPayload);
            Assert.Equal(entries.Length, response.Endpoints.Count);
        }

        private async Task UnpublishAllNodesAsync(CancellationToken ct = default)
        {
            MethodResultModel result = null;
            for (var i = 0; i < 5; i++)
            {
                result = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.UnpublishAllNodes,
                        JsonPayload = "null"
                    },
                    ct).ConfigureAwait(false);

                if (result.Status == 405)
                {
                    _context.OutputHelper?.WriteLine(result.JsonPayload);
                    await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct);
                    continue;
                }
                break;
            }
            Assert.Equal((int)HttpStatusCode.OK, result?.Status);

            var result1 = await CallMethodAsync(
                new MethodParameterModel
                {
                    Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                },
                ct).ConfigureAwait(false);

            Assert.Equal((int)HttpStatusCode.OK, result1.Status);
            var response = _serializer.Deserialize<GetConfiguredEndpointsResponseModel>(result1.JsonPayload);
            Assert.Empty(response.Endpoints);
        }

        private const string kModuleName = "publisher_standalone_cs";
        private const string kDeploymentName = "__default-opcpublisher-standalone-cs";
    }
}
