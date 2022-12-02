// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests {
    using IIoTPlatform_E2E_Tests.TestEventProcessor;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Renci.SshNet;
    using RestSharp;
    using RestSharp.Authenticators;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;

    internal static partial class TestHelper {

        /// <summary>
        /// Request OAuth token using Http basic authentication from environment variables
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Return content of request token or empty string</returns>
        public static async Task<string> GetTokenAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        ) {
            return await GetTokenAsync(
                context.IIoTPlatformConfigHubConfig.AuthTenant,
                context.IIoTPlatformConfigHubConfig.AuthClientId,
                context.IIoTPlatformConfigHubConfig.AuthClientSecret,
                context.IIoTPlatformConfigHubConfig.ApplicationName,
                ct
            );
        }

        /// <summary>
        /// Request OAuth token using Http basic authentication
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="clientId">User name for HTTP basic authentication</param>
        /// <param name="clientSecret">Password for HTTP basic authentication</param>
        /// <param name="applicationName">Name of deployed Industrial IoT</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Return content of request token or empty string</returns>
        public static async Task<string> GetTokenAsync(
            string tenantId,
            string clientId,
            string clientSecret,
            string applicationName,
            CancellationToken ct = default
        ) {
            Assert.True(!string.IsNullOrWhiteSpace(tenantId), "tenantId is null");
            Assert.True(!string.IsNullOrWhiteSpace(clientId), "clientId is null");
            Assert.True(!string.IsNullOrWhiteSpace(clientSecret), "clientSecret is null");
            Assert.True(!string.IsNullOrWhiteSpace(applicationName), "applicationName is null");

            var client = new RestClient($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token") {
                Authenticator = new HttpBasicAuthenticator(clientId, clientSecret),
            };

            var request = new RestRequest("", Method.Post) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds,
            };
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("scope", $"api://{tenantId}/{applicationName}-service/.default");

            var response = await client.ExecuteAsync(request, ct);
            Assert.True(response.IsSuccessful, $"Request OAuth2.0 failed, Status {response.StatusCode}, ErrorMessage: {response.ErrorMessage}");
            dynamic json = JsonConvert.DeserializeObject(response.Content);
            Assert.NotNull(json);
            Assert.NotEmpty(json);
            return $"{json.token_type} {json.access_token}";
        }

        /// <summary>
        /// Get urls of the simulated test opc servers
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <returns>List of server urls</returns>
        public static List<string> GetSimulatedOpcServerUrls(
            IIoTPlatformTestContext context) {
            return context.OpcPlcConfig.Urls.Split(TestConstants.SimulationUrlsSeparator).Select(ip => $"opc.tcp://{ip}:50000").ToList();
        }

        /// <summary>
        /// Read PublishedNodes json from OPC-PLC and provide the data to the tests
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Dictionary with URL of PLC-PLC as key and Content of Published Nodes files as value</returns>
        public static async Task<IDictionary<string, PublishedNodesEntryModel>> GetSimulatedPublishedNodesConfigurationAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        ) {
            var result = new Dictionary<string, PublishedNodesEntryModel>();

            var opcPlcList = context.OpcPlcConfig.Urls;
            context.OutputHelper?.WriteLine($"SimulatedOpcPlcUrls {opcPlcList}");
            var ipAddressList = opcPlcList.Split(TestConstants.SimulationUrlsSeparator);

            foreach (var ipAddress in ipAddressList.Where(s => !string.IsNullOrWhiteSpace(s))) {
                try {
                    using (var client = new HttpClient()) {
                        var ub = new UriBuilder { Host = ipAddress };
                        var baseAddress = ub.Uri;

                        client.BaseAddress = baseAddress;
                        client.Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds);

                        using (var response = await client.GetAsync(TestConstants.OpcSimulation.PublishedNodesFile, ct)) {
                            Assert.NotNull(response);
                            Assert.True(response.IsSuccessStatusCode, $"http GET request to load pn.json failed, Status {response.StatusCode}");
                            var json = await response.Content.ReadAsStringAsync(ct);
                            Assert.NotEmpty(json);
                            var entryModels = JsonConvert.DeserializeObject<PublishedNodesEntryModel[]>(json);

                            Assert.NotNull(entryModels);
                            Assert.NotEmpty(entryModels);
                            Assert.NotNull(entryModels[0].OpcNodes);
                            Assert.NotEmpty(entryModels[0].OpcNodes);

                            // Set endpoint url correctly when it's not specified in pn.json ie. replace fqdn with the ip address
                            string fqdn = Regex.Match(entryModels[0].EndpointUrl, @"opc.tcp:\/\/([^\}]+):").Groups[1].Value;
                            entryModels[0].EndpointUrl = entryModels[0].EndpointUrl.Replace(fqdn, ipAddress);

                            result.Add(ipAddress, entryModels[0]);
                        }
                    }
                }
                catch (Exception e) {
                    context.OutputHelper?.WriteLine("Error occurred while downloading Message: {0} skipped: {1}", e.Message, ipAddress);
                    continue;
                }
            }
            return result;
        }

        /// <summary>
        /// Update Device Twin tag
        /// </summary>
        /// <param name="patch">Name of deployed Industrial IoT</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task UpdateTagAsync(
            string patch,
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        ) {
            var registryManager = context.RegistryHelper.RegistryManager;
            var twin = await registryManager.GetTwinAsync(context.DeviceConfig.DeviceId, ct);
            await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag, ct);
        }

        /// <summary>
        /// Call rest API
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="method">REST method (Get, Post, Delete...)</param>
        /// <param name="route">Route for the url</param>
        /// <param name="body">Body for the request</param>
        /// <param name="queryParameters">Additional query parameters</param>
        /// /// <param name="ct">Cancellation token</param>
        public static RestResponse CallRestApi(
            IIoTPlatformTestContext context,
            Method method,
            string route,
            object body = null,
            Dictionary<string, string> queryParameters = null,
            CancellationToken ct = default
        ) {
            var accessToken = GetTokenAsync(context, ct).GetAwaiter().GetResult();

            var request = new RestRequest(route, method) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds,
            };
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);

            if (body != null) {
                request.AddJsonBody(body);
            }

            if (queryParameters != null) {
                foreach (var param in queryParameters) {
                    request.AddQueryParameter(param.Key, param.Value);
                }
            }

            var restClient = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);
            var response = restClient.ExecuteAsync(request, ct).GetAwaiter().GetResult();
            return response;
        }

        /// <summary>
        /// Transfer the content of published_nodes.json file into the OPC Publisher edge module
        /// </summary>
        /// <param name="json">String for published_nodes.json</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task PublishNodesAsync(
            string json,
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        ) {
            CreateFolderOnEdgeVM(TestConstants.PublishedNodesFolder, context);
            using var scpClient = CreateScpClientAndConnect(context);
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            scpClient.Upload(stream, TestConstants.PublishedNodesFullName);

            if (context.IoTEdgeConfig.NestedEdgeFlag == "Enable") {
                using var sshCient = CreateSshClientAndConnect(context);
                foreach (var edge in context.IoTEdgeConfig.NestedEdgeSshConnections) {
                    if (edge != string.Empty) {
                        // Copy file to the edge vm
                        var command = $"scp -oStrictHostKeyChecking=no {TestConstants.PublishedNodesFullName} {edge}:{TestConstants.PublishedNodesFilename}";
                        sshCient.RunCommand(command);
                        // Move file to the target folder with sudo permissions
                        command = $"ssh -oStrictHostKeyChecking=no {edge} 'sudo mv {TestConstants.PublishedNodesFilename} {TestConstants.PublishedNodesFullName}'";
                        sshCient.RunCommand(command);
                    }
                }
            }
        }

        /// <summary>
        /// Transfer the content of published_nodes.json file into the OPC Publisher edge module
        /// </summary>
        /// <param name="entries">Entries for published_nodes.json</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task PublishNodesAsync(
            IIoTPlatformTestContext context,
            IEnumerable<PublishedNodesEntryModel> entries,
            CancellationToken ct = default
        ) {
            var json = JsonConvert.SerializeObject(entries, Formatting.Indented);

            context.OutputHelper?.WriteLine("Write published_nodes.json to IoT Edge");
            context.OutputHelper?.WriteLine(JsonConvert.SerializeObject(entries));

            await PublishNodesAsync(json, context, ct);
        }

        /// <summary>
        /// Clean published nodes JSON files.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static async Task CleanPublishedNodesJsonFilesAsync(IIoTPlatformTestContext context) {
            // Make sure directories exist.
            using (var sshCient = CreateSshClientAndConnect(context)) {
                sshCient.RunCommand($"[ ! -d {TestConstants.PublishedNodesFolder} ]" +
                    $" && sudo mkdir -m 777 -p {TestConstants.PublishedNodesFolder}");
            }
            await PublishNodesAsync(
                context,
                Array.Empty<PublishedNodesEntryModel>()
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the unmanaged-Tag to "true" to enable Standalone-Mode
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns></returns>
        public static async Task SwitchToStandaloneModeAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        ) {
            var patch =
                @"{
                    tags: {
                        unmanaged: true
                    }
                }";

            await UpdateTagAsync(patch, context, ct);
        }

        /// <summary>
        /// Sets the unmanaged-Tag to "true" to enable Orchestrated-Mode
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns></returns>
        public static async Task SwitchToOrchestratedModeAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        ) {
            var patch =
                @"{
                    tags: {
                        unmanaged: null
                    }
                }";

            await UpdateTagAsync(patch, context, ct);

            DeleteFileOnEdgeVM(TestConstants.PublishedNodesFullName, context);
        }

        /// <summary>
        /// Create a new SshClient based on SshConfig and directly connects to host
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <returns>Instance of SshClient, that need to be disposed</returns>
        private static SshClient CreateSshClientAndConnect(IIoTPlatformTestContext context) {
            var privateKeyFile = GetPrivateSshKey(context);
            try {
                var client = new SshClient(
                    context.SshConfig.Host,
                    context.SshConfig.Username,
                    privateKeyFile);

                var connectAttempt = 0;
                while (true) {
                    try {
                        client.Connect();
                        return client;
                    }
                    catch (SocketException) when (++connectAttempt < 5) {
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex) {
                context.OutputHelper?.WriteLine("Failed to open ssh connection to host {0} with username {1} ({2})",
                    context.SshConfig.Host,
                    context.SshConfig.Username, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Create a new ScpClient based on SshConfig and directly connects to host
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <returns>Instance of SshClient, that need to be disposed</returns>
        private static ScpClient CreateScpClientAndConnect(IIoTPlatformTestContext context) {
            var privateKeyFile = GetPrivateSshKey(context);
            try {
                var client = new ScpClient(
                    context.SshConfig.Host,
                    context.SshConfig.Username,
                    privateKeyFile);

                var connectAttempt = 0;
                while (true) {
                    try {
                        client.Connect();
                        return client;
                    }
                    catch (SocketException) when (++connectAttempt < 5) {
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex) {
                context.OutputHelper?.WriteLine("Failed to open scp connection to host {0} with username {1} ({2})",
                    context.SshConfig.Host,
                    context.SshConfig.Username, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets the private SSH key from the configuration to connect to the Edge VM
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <returns></returns>
        private static PrivateKeyFile GetPrivateSshKey(IIoTPlatformTestContext context) {
            var buffer = Encoding.Default.GetBytes(context.SshConfig.PrivateKey);
            var privateKeyStream = new MemoryStream(buffer);

            var privateKeyFile = new PrivateKeyFile(privateKeyStream);
            return privateKeyFile;
        }

        /// <summary>
        /// Delete a file on the Edge VM
        /// </summary>
        /// <param name="fileName">Filename of the file to delete</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        public static void DeleteFileOnEdgeVM(string fileName, IIoTPlatformTestContext context) {
            var isSuccessful = false;
            using var client = CreateSshClientAndConnect(context);

            var terminal = client.RunCommand("rm " + fileName);

            if (string.IsNullOrEmpty(terminal.Error) || terminal.Error.ToLowerInvariant().Contains("no such file")) {
                isSuccessful = true;
            }
            Assert.True(isSuccessful, "Delete file was not successful");

            if (context.IoTEdgeConfig.NestedEdgeFlag == "Enable") {
                using var sshCient = CreateSshClientAndConnect(context);
                foreach (var edge in context.IoTEdgeConfig.NestedEdgeSshConnections) {
                    if (edge != string.Empty) {
                        var command = $"ssh -oStrictHostKeyChecking=no {edge} 'sudo rm {fileName}'";
                        sshCient.RunCommand(command);
                    }
                }
            }
        }

        /// <summary>
        /// Create a folder on Edge VM (if not exists)
        /// </summary>
        /// <param name="folderPath">Name of the folder to create.</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        private static void CreateFolderOnEdgeVM(string folderPath, IIoTPlatformTestContext context) {
            Assert.True(!string.IsNullOrWhiteSpace(folderPath), "folder does not exist");

            var isSuccessful = false;
            using var client = CreateSshClientAndConnect(context);

            var terminal = client.RunCommand("sudo mkdir " + folderPath + ";" + "cd " + folderPath + "; " + "sudo chmod 777 " + folderPath);

            if (string.IsNullOrEmpty(terminal.Error) || terminal.Error.Contains("File exists")) {
                isSuccessful = true;
            }

            Assert.True(isSuccessful, "Folder creation was not successful");
        }

        /// <summary>
        /// Starts monitoring the incoming messages of the IoT Hub and checks for missing values.
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="expectedValuesChangesPerTimestamp">The expected number of value changes per timestamp</param>
        /// <param name="expectedIntervalOfValueChanges">The expected time difference between values changes in milliseconds</param>
        /// <param name="expectedMaximalDuration">The time difference between OPC UA Server fires event until Changes Received in IoT Hub in milliseconds </param>
        /// <param name="ct">Cancellation token</param>
        /// <returns></returns>
        public static async Task StartMonitoringIncomingMessagesAsync(
            IIoTPlatformTestContext context,
            int expectedValuesChangesPerTimestamp,
            int expectedIntervalOfValueChanges,
            int expectedMaximalDuration,
            CancellationToken ct = default
        ) {
            var runtimeUrl = context.TestEventProcessorConfig.TestEventProcessorBaseUrl.TrimEnd('/') + "/Runtime";

            var client = new RestClient(runtimeUrl) {
                Authenticator = new HttpBasicAuthenticator(context.TestEventProcessorConfig.TestEventProcessorUsername,
                    context.TestEventProcessorConfig.TestEventProcessorPassword),
            };

            var body = new {
                CommandType = CommandEnum.Start,
                Configuration = new {
                    IoTHubEventHubEndpointConnectionString = context.IoTHubConfig.IoTHubEventHubConnectionString,
                    StorageConnectionString = context.IoTHubConfig.CheckpointStorageConnectionString,
                    ExpectedValueChangesPerTimestamp = expectedValuesChangesPerTimestamp,
                    ExpectedIntervalOfValueChanges = expectedIntervalOfValueChanges,
                    ThresholdValue = expectedIntervalOfValueChanges > 0
                        ? expectedIntervalOfValueChanges / 10
                        : 100,
                    ExpectedMaximalDuration = expectedMaximalDuration,
                }
            };

            var request = new RestRequest("", Method.Put) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds,
            };
            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request, ct);
            Assert.True(response.IsSuccessful, $"Response status code, Status {response.StatusCode}, ErrorMessage: {response.ErrorMessage}");
            context.OutputHelper?.WriteLine("Monitoring events started!");

            dynamic json = JsonConvert.DeserializeObject(response.Content);
            Assert.NotNull(json);
        }

        /// <summary>
        /// Stops the monitoring of incoming event to an IoT Hub and returns success/failure.
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns></returns>
        public static async Task<StopResult> StopMonitoringIncomingMessagesAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        ) {
            // TODO Merge with Start-Method to avoid code duplication
            var runtimeUrl = context.TestEventProcessorConfig.TestEventProcessorBaseUrl.TrimEnd('/') + "/Runtime";

            var client = new RestClient(runtimeUrl) {
                Authenticator = new HttpBasicAuthenticator(context.TestEventProcessorConfig.TestEventProcessorUsername,
                    context.TestEventProcessorConfig.TestEventProcessorPassword),
            };

            var body = new {
                CommandType = CommandEnum.Stop,
            };

            var request = new RestRequest("", Method.Put) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds,
            };
            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request, ct);
            context.OutputHelper?.WriteLine("Monitoring events stopped!");

            var result = JsonConvert.DeserializeObject<StopResult>(response.Content);
            Assert.NotNull(result);

            return result;
        }

        /// <summary>
        /// Wait for all API services of IIoT platform to be healthy.
        /// </summary>
        /// <param name="context"> Shared Context for E2E testing Industrial IoT Platform </param>
        /// <param name="ct"> Cancellation token </param>
        /// <returns></returns>
        public static async Task WaitForServicesAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        ) {
            const string healthyState = "Healthy";

            var healthRoutes = new string[] {
                TestConstants.APIRoutes.RegistryHealth,
                TestConstants.APIRoutes.PublisherHealth,
                TestConstants.APIRoutes.TwinHealth,
                TestConstants.APIRoutes.JobOrchestratorHealth
            };

            try {
                var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

                while (true) {
                    ct.ThrowIfCancellationRequested();

                    var tasks = new List<Task<RestResponse>>();

                    foreach (var healthRoute in healthRoutes) {
                        var request = new RestRequest(healthRoute, Method.Get) {
                            Timeout = TestConstants.DefaultTimeoutInMilliseconds,
                        };

                        tasks.Add(client.ExecuteAsync(request, ct));
                    }

                    await Task.WhenAll(tasks.ToArray());

                    var healthyServices = tasks
                        .Where(task => task.Result.StatusCode == HttpStatusCode.OK)
                        .Count(task => task.Result.Content == healthyState);

                    if (healthyServices == healthRoutes.Length) {
                        context.OutputHelper?.WriteLine("All API microservices of IIoT platform " +
                            "are running and in healthy state.");
                        return;
                    }

                    await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) {
                context.OutputHelper?.WriteLine("Error: not all API microservices of IIoT " +
                    $"platform are in healthy state ({e.Message}).");
                throw;
            }
        }

        /// <summary>
        /// Determines if two strings can be considered the representation of the same URL
        /// </summary>
        /// <param name="url1">URL to compare</param>
        /// <param name="url2">URL to compare to</param>
        public static bool IsUrlStringsEqual(string url1, string url2) =>
            string.Equals(url1?.TrimEnd('/'), url2?.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines if an ExpandoObject has a property
        /// </summary>
        /// <param name="expandoObject">ExpandoObject to exemine</param>
        /// <param name="propertyName">Name of the property</param>
        public static bool HasProperty(object expandoObject, string propertyName) {
            if (!(expandoObject is IDictionary<string, object> dictionary)) {
                throw new InvalidOperationException("Object is not an ExpandoObject");
            }
            return dictionary.ContainsKey(propertyName);
        }

        /// <summary>
        /// Create a single node model with opcplc node
        /// </summary>
        /// <param name="IIoTMultipleNodesTestContext">context</param>
        /// <param name="CancellationTokenSource">cancellation token</param>
        public static async Task<PublishedNodesEntryModel> CreateSingleNodeModelAsync(IIoTMultipleNodesTestContext context, CancellationToken ct, DataChangeTriggerType? dataChangeTrigger = null) {
            IDictionary<string, PublishedNodesEntryModel> simulatedPublishedNodesConfiguration =
                new Dictionary<string, PublishedNodesEntryModel>(0);

            // With the nested edge test servers don't have public IP addresses and cannot be accessed in this way
            if (context.IoTEdgeConfig.NestedEdgeFlag != "Enable") {
                simulatedPublishedNodesConfiguration =
                    await GetSimulatedPublishedNodesConfigurationAsync(context, ct);
            }

            PublishedNodesEntryModel model;
            if (simulatedPublishedNodesConfiguration.Count > 0) {
                model = simulatedPublishedNodesConfiguration[simulatedPublishedNodesConfiguration.Keys.First()];
            }
            else {
                var opcPlcIp = context.OpcPlcConfig.Urls.Split(TestConstants.SimulationUrlsSeparator)[0];
                model = new PublishedNodesEntryModel {
                    EndpointUrl = $"opc.tcp://{opcPlcIp}:50000",
                    UseSecurity = false,
                    OpcNodes = new OpcUaNodesModel[] {
                        new OpcUaNodesModel {
                            Id = "ns=2;s=SlowUInt1",
                            OpcPublishingInterval = 10000,
                        }
                    }
                };
            }

            // We want to take one of the slow nodes that updates each 10 seconds.
            // To make sure that we will not have missing values because of timing issues,
            // we will set publishing and sampling intervals to a lower value than the publishing
            // interval of the simulated OPC PLC. This will eliminate false-positives.
            model.OpcNodes = model.OpcNodes
                .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                .Where(opcNode => opcNode.Id.Contains("SlowUInt"))
                .Take(1).Select(opcNode => {
                    var opcPlcPublishingInterval = opcNode.OpcPublishingInterval;
                    opcNode.OpcPublishingInterval = opcPlcPublishingInterval / 2;
                    opcNode.OpcSamplingInterval = opcPlcPublishingInterval / 4;
                    opcNode.QueueSize = 4;
                    opcNode.DataChangeTrigger = dataChangeTrigger == null ? null : dataChangeTrigger.ToString();
                    return opcNode;
                })
                .ToArray();

            return model;
        }

        /// <summary>
        /// Create a multiple nodes model with opcplc nodes
        /// </summary>
        /// <param name="IIoTMultipleNodesTestContext">context</param>
        /// <param name="CancellationTokenSource">cancellation token</param>
        public static async Task<PublishedNodesEntryModel> CreateMultipleNodesModelAsync(
            IIoTMultipleNodesTestContext context,
            CancellationToken ct,
            int endpointIndex = 2,
            int numberOfNodes = 250) {

            await context.LoadSimulatedPublishedNodes(ct);

            PublishedNodesEntryModel nodesToPublish;
            if (context.SimulatedPublishedNodes.Count > 1) {
                var testPlc = context.SimulatedPublishedNodes.Skip(endpointIndex).First().Value;
                nodesToPublish = context.GetEntryModelWithoutNodes(testPlc);

                // We want to take several slow and fast nodes.
                // To make sure that we will not have missing values because of timing issues,
                // we will set publishing and sampling intervals to a lower value than the publishing
                // interval of the simulated OPC PLC. This will eliminate false-positives.
                nodesToPublish.OpcNodes = testPlc.OpcNodes
                    .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                    .Where(node => node.Id.Contains("slow", StringComparison.OrdinalIgnoreCase)
                        || node.Id.Contains("fast", StringComparison.OrdinalIgnoreCase))
                    .Take(numberOfNodes)
                    .Select(opcNode => {
                        var opcPlcPublishingInterval = opcNode.OpcPublishingInterval;
                        opcNode.OpcPublishingInterval = opcPlcPublishingInterval / 2;
                        opcNode.OpcSamplingInterval = opcPlcPublishingInterval / 4;
                        opcNode.QueueSize = 4;
                        return opcNode;
                    })
                    .ToArray();

                context.ConsumedOpcUaNodes.AddOrUpdate(testPlc.EndpointUrl, nodesToPublish);
            }
            else {
                var opcPlcIp = context.OpcPlcConfig.Urls.Split(TestConstants.SimulationUrlsSeparator)[endpointIndex];
                nodesToPublish = new PublishedNodesEntryModel {
                    EndpointUrl = $"opc.tcp://{opcPlcIp}:50000",
                    UseSecurity = false
                };

                var nodes = new List<OpcUaNodesModel>();
                for (int i = 0; i < numberOfNodes; i++) {
                    nodes.Add(new OpcUaNodesModel {
                        Id = $"ns=2;s=SlowUInt{i + 1}",
                        OpcPublishingInterval = 10000 / 2,
                        OpcSamplingInterval = 10000 / 4,
                        QueueSize = 4,
                    });
                }

                nodesToPublish.OpcNodes = nodes.ToArray();
                context.ConsumedOpcUaNodes.Add(opcPlcIp, nodesToPublish);
            }

            return nodesToPublish;
        }


        /// <summary>
        /// Initialize DeviceServiceClient from IoT Hub connection string.
        /// </summary>
        /// <param name="iotHubConnectionString"></param>
        /// <param name="transportType"></param>
        public static ServiceClient DeviceServiceClient(
            string iotHubConnectionString,
            Microsoft.Azure.Devices.TransportType transportType = Microsoft.Azure.Devices.TransportType.Amqp_WebSocket_Only
        ) {
            ServiceClient iotHubClient;

            if (string.IsNullOrWhiteSpace(iotHubConnectionString)) {
                throw new ArgumentNullException(nameof(iotHubConnectionString));
            }

            return iotHubClient = ServiceClient.CreateFromConnectionString(
                iotHubConnectionString,
                transportType
            );
        }

        /// <summary>
        /// Gets endpoints from registry
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        private static async Task<dynamic> GetEndpointsInternalAsync(IIoTPlatformTestContext context, CancellationToken ct) {
            var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
            var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

            var request = new RestRequest(TestConstants.APIRoutes.RegistryEndpoints, Method.Get) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds,
            };
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);

            var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, $"GET /registry/v2/endpoints failed ({response.StatusCode}, {response.ErrorMessage})!");
            return JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
        }

        /// <summary>
        /// Gets applications from registry
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        private static async Task<dynamic> GetApplicationsInternalAsync(IIoTPlatformTestContext context, CancellationToken ct) {
            var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
            var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

            var request = new RestRequest(TestConstants.APIRoutes.RegistryApplications, Method.Get) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds,
            };
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);

            var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
            Assert.NotNull(response);
            Assert.True(response.IsSuccessful, $"GET /registry/v2/applications failed ({response.StatusCode}, {response.ErrorMessage})!");

            return JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
        }

        /// <summary>
        /// Prints the exception message and stacktrace for exception (and all inner exceptions) in test output
        /// </summary>
        /// <param name="e">Exception to be printed</param>
        /// <param name="outputHelper">XUnit Test OutputHelper instance or null (no print in this case)</param>
        private static void PrettyPrintException(Exception e, ITestOutputHelper outputHelper) {

            var exception = e;
            while (exception != null) {
                outputHelper.WriteLine(exception.Message);
                outputHelper.WriteLine(exception.StackTrace);
                outputHelper.WriteLine("");
                exception = exception.InnerException;
            }
        }

        /// <summary>
        /// Call a direct method
        /// </summary>
        /// <param name="serviceClient">Device service client</param>
        /// <param name="deviceId">Device Id</param>
        /// <param name="moduleId">Module Id</param>
        /// <param name="parameters">Method parameter </param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task<MethodResultModel> CallMethodAsync(ServiceClient serviceClient, string deviceId, string moduleId,
            MethodParameterModel parameters, IIoTPlatformTestContext context, CancellationToken ct) {
            var attempt = 0;
            while (true) {
                try {
                    var methodInfo = new CloudToDeviceMethod(parameters.Name);
                    methodInfo.SetPayloadJson(parameters.JsonPayload);
                    var result = await (string.IsNullOrEmpty(moduleId) ?
                         serviceClient.InvokeDeviceMethodAsync(deviceId, methodInfo, ct) :
                         serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInfo, ct));
                    context.OutputHelper.WriteLine($"Called method {parameters.Name}.");
                    return new MethodResultModel {
                        JsonPayload = result.GetPayloadAsJson(),
                        Status = result.Status
                    };
                }
                catch (Exception e) {
                    context.OutputHelper.WriteLine($"Failed to call method {parameters.Name} with {parameters.JsonPayload}");
                    if (e.Message.Contains("The operation failed because the requested device isn't online") && ++attempt < 60) {
                        context.OutputHelper.WriteLine("Device is not online, trying again to call device after delay...");
                        await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct).ConfigureAwait(false);
                        continue;
                    }
                    PrettyPrintException(e, context.OutputHelper);
                    throw;
                }
            }
        }
    }
}
