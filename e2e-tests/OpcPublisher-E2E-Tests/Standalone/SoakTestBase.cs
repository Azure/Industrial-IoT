// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.Standalone
{
    using OpcPublisherAEE2ETests.Deploy;
    using OpcPublisherAEE2ETests.TestExtensions;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Testing.Telemetry;
    using Azure.Messaging.EventHubs.Consumer;
    using Microsoft.Azure.Devices;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// <para>
    /// Base class for the long running telemetry quality tests. Each derived
    /// scenario deploys its <em>own</em> OPC Publisher module into the shared
    /// IoT Edge gateway and creates its <em>own</em> OPC PLC simulation, so
    /// the scenarios can run in parallel with each other and with the A&amp;E
    /// tests without contending for the published nodes file, the pki store,
    /// the simulation server or the event hub consumer group.
    /// </para>
    /// <para>
    /// Reusing the deployed resource group, IoT Hub and edge VM rather than
    /// standing up a second set is deliberate: the code under test is the
    /// publisher module, and duplicating the (slow, expensive) infrastructure
    /// would add cost and cleanup risk without adding coverage.
    /// </para>
    /// </summary>
    public abstract class SoakTestBase : IDisposable
    {
        /// <summary>
        /// Shared test context
        /// </summary>
        protected IIoTStandaloneTestContext Context { get; }

        /// <summary>
        /// Cancellation for the whole scenario
        /// </summary>
        protected CancellationToken TimeoutToken { get; }

        /// <summary>
        /// Deployment of this scenario's publisher module
        /// </summary>
        protected IoTHubPublisherDeployment Deployment { get; }

        /// <summary>
        /// Writer id used to configure and recognize this scenario's data
        /// </summary>
        protected string WriterId { get; }

        /// <summary>
        /// How long telemetry is observed after the warm up
        /// </summary>
        protected static TimeSpan Duration => TestConstants.Soak.Duration;

        /// <summary>
        /// Create the scenario
        /// </summary>
        /// <param name="context"></param>
        /// <param name="output"></param>
        /// <param name="moduleName">Module identity of this scenario's publisher</param>
        /// <param name="deploymentName">Layered deployment name, must be unique</param>
        /// <param name="consumerGroup">Dedicated event hub consumer group</param>
        protected SoakTestBase(IIoTStandaloneTestContext context, ITestOutputHelper output,
            string moduleName, string deploymentName, string consumerGroup)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Context.SetOutputHelper(output);
            _output = output;

            //
            // The default ten minute test timeout cannot bound a soak. Allow
            // the observation window plus enough headroom for deploying the
            // module, creating the simulation container and tearing both down.
            //
            _timeoutTokenSource = new CancellationTokenSource(Duration + kOverhead);
            TimeoutToken = _timeoutTokenSource.Token;

            WriterId = Guid.NewGuid().ToString();
            _consumer = Context.GetEventHubConsumerClient(consumerGroup);

            Deployment = new IoTHubPublisherDeployment(Context,
                OpcPublisherAEE2ETests.MessagingMode.PubSub,
                moduleName: moduleName,
                deploymentName: deploymentName,
                publishedNodesFile: TestConstants.PublishedNodesFolder + "/published_nodes_" + moduleName + ".json",
                pkiPath: TestConstants.PublishedNodesFolder + "/pki_" + moduleName,
                createFileIfNotExist: true);

            _iotHubClient = TestHelper.DeviceServiceClient(
                Context.IoTHubConfig.IoTHubConnectionString,
                Microsoft.Azure.Devices.TransportType.Amqp_WebSocket_Only);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _consumer?.CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
                _consumer?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _iotHubClient?.Dispose();
                _timeoutTokenSource?.Dispose();
            }
        }

        /// <summary>
        /// Deploy this scenario's publisher module and wait until it serves
        /// direct methods.
        /// </summary>
        protected async Task DeployPublisherAsync()
        {
            await Context.RegistryHelper.DeployStandalonePublisherAsync(
                Deployment, TimeoutToken).ConfigureAwait(false);

            //
            // Reaching IoT Hub "Connected" state does not guarantee the freshly
            // deployed module is ready to serve direct methods: its handlers
            // and configuration services may still be initializing.
            //
            await WaitUntilPublisherReadyAsync(TimeoutToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Build the published nodes configuration for a set of counter nodes.
        /// </summary>
        /// <param name="nodeIds">Node ids to publish</param>
        /// <param name="heartbeatInterval">Heartbeat interval</param>
        protected string PublishedNodes(IEnumerable<string> nodeIds, TimeSpan heartbeatInterval)
        {
            var nodes = new JArray();
            foreach (var nodeId in nodeIds)
            {
                nodes.Add(new JObject(
                    new JProperty("Id", nodeId),
                    new JProperty("OpcPublishingInterval",
                        (int)TestConstants.Soak.PublishingInterval.TotalMilliseconds),
                    new JProperty("OpcSamplingInterval",
                        (int)TestConstants.Soak.PublishingInterval.TotalMilliseconds),
                    new JProperty("HeartbeatInterval", (int)heartbeatInterval.TotalSeconds),
                    new JProperty("QueueSize", TestConstants.Soak.QueueSize)));
            }
            return Context.PublishedNodesJson(TestConstants.OpcSimulation.Port, WriterId, nodes);
        }

        /// <summary>
        /// Observe the telemetry of this scenario's publisher module and feed
        /// it into a validator.
        /// </summary>
        /// <param name="options">Validator configuration</param>
        /// <param name="warmup">
        /// Time given to the publisher to create all monitored items and to
        /// settle before the stream is analysed.
        /// </param>
        protected async Task<TelemetryQualityReport> ObserveAsync(
            TelemetryQualityOptions options, TimeSpan warmup)
        {
            var validator = new TelemetryQualityValidator(options);
            var stopWatch = Stopwatch.StartNew();
            var total = await _consumer.ConsumeAsync(Deployment.ModuleName, warmup + Duration,
                message =>
                {
                    if (stopWatch.Elapsed < warmup)
                    {
                        return;
                    }
                    //
                    // 3.0 publishes OPC UA PubSub network messages. The samples
                    // encoding this used to read was the MonitoredItemMessage
                    // format, which was removed with the Samples messaging mode
                    // because it has no representation in Part 14.
                    //
                    validator.AddPubSubMessage(message);
                },
                onFirstMessage: json => _output.WriteLine("First message:" +
                    Environment.NewLine + json),
                TimeoutToken).ConfigureAwait(false);

            var report = validator.CreateReport();
            _output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"--- {Deployment.ModuleName}: {total} message(s), {warmup} warm up, " +
                $"{Duration} analysed ---"));
            _output.WriteLine(report.ToString());
            return report;
        }

        /// <summary>
        /// Remove this scenario's simulation container and layered deployment.
        /// </summary>
        protected async Task CleanupAsync()
        {
            await TestHelper.DeleteSimulationContainerAsync(Context, TimeoutToken)
                .ConfigureAwait(false);
            await Deployment.DeleteLayeredDeploymentAsync(TimeoutToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Publish the given configuration through direct methods.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="ct"></param>
        protected async Task PublishNodesAsync(string json, CancellationToken ct)
        {
            await UnpublishAllNodesAsync(ct).ConfigureAwait(false);
            var entries = JsonConvert.DeserializeObject<PublishedNodesEntryModel[]>(json);
            foreach (var entry in entries)
            {
                var result = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.PublishNodes,
                        JsonPayload = JsonConvert.SerializeObject(entry)
                    }, ct).ConfigureAwait(false);

                await AssertMethodStatusOkAsync(result, "PublishNodes", ct).ConfigureAwait(false);
            }

            var configured = await CallMethodAsync(
                new MethodParameterModel
                {
                    Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                }, ct).ConfigureAwait(false);

            await AssertMethodStatusOkAsync(configured, "GetConfiguredEndpoints", ct)
                .ConfigureAwait(false);
            var response = JsonConvert.DeserializeObject<GetConfiguredEndpointsResponseModel>(
                configured.JsonPayload);
            Assert.Equal(entries.Length, response.Endpoints.Count);
        }

        /// <summary>
        /// Remove all configuration from this scenario's publisher.
        /// </summary>
        /// <param name="ct"></param>
        protected async Task UnpublishAllNodesAsync(CancellationToken ct = default)
        {
            MethodResultModel result = null;
            for (var i = 0; i < 5; i++)
            {
                result = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.UnpublishAllNodes,
                        JsonPayload = "null"
                    }, ct).ConfigureAwait(false);

                if (result.Status == 405)
                {
                    // Retry if method not yet mounted
                    Context.OutputHelper?.WriteLine(result.JsonPayload);
                    await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct)
                        .ConfigureAwait(false);
                    continue;
                }
                break;
            }
            await AssertMethodStatusOkAsync(result, "UnpublishAllNodes", ct).ConfigureAwait(false);
        }

        private async Task<MethodResultModel> CallMethodAsync(MethodParameterModel parameters,
            CancellationToken ct)
        {
            return await TestHelper.CallMethodAsync(_iotHubClient,
                Context.DeviceConfig.DeviceId, Deployment.ModuleName, parameters,
                Context, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Poll a benign read-only direct method until the module answers.
        /// </summary>
        /// <param name="ct"></param>
        private async Task WaitUntilPublisherReadyAsync(CancellationToken ct)
        {
            while (true)
            {
                var result = await CallMethodAsync(
                    new MethodParameterModel
                    {
                        Name = TestConstants.DirectMethodNames.GetConfiguredEndpoints
                    }, ct).ConfigureAwait(false);

                if (result.Status == (int)HttpStatusCode.OK)
                {
                    return;
                }

                Context.OutputHelper?.WriteLine(
                    $"Publisher {Deployment.ModuleName} not ready yet " +
                    $"(status {result.Status}), retrying...");
                await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Assert a direct method succeeded, dumping the module logs on failure
        /// so the server side exception is visible without another billable
        /// end to end cycle.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="methodName"></param>
        /// <param name="ct"></param>
        private async Task AssertMethodStatusOkAsync(MethodResultModel result,
            string methodName, CancellationToken ct)
        {
            if (result?.Status != (int)HttpStatusCode.OK)
            {
                var logs = await TestHelper.GetModuleLogsAsync(
                    Context, Deployment.ModuleName, ct: ct).ConfigureAwait(false);
                Context.OutputHelper?.WriteLine(
                    $"{methodName} failed with status {result?.Status}: {result?.JsonPayload}" +
                    $"{Environment.NewLine}=== {Deployment.ModuleName} module logs ==={Environment.NewLine}{logs}");
            }
            Assert.Equal((int)HttpStatusCode.OK, result?.Status);
        }

        /// <summary>
        /// Headroom on top of the observation window for deploying the module,
        /// creating the simulation container and tearing both down again.
        /// </summary>
        private static readonly TimeSpan kOverhead = TimeSpan.FromMinutes(45);
        private readonly ITestOutputHelper _output;
        private readonly ServiceClient _iotHubClient;
        private readonly EventHubConsumerClient _consumer;
        private readonly CancellationTokenSource _timeoutTokenSource;
    }
}
