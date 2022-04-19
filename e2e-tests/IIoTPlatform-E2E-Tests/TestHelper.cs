// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests {
    using Newtonsoft.Json;
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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs.Consumer;
    using IIoTPlatform_E2E_Tests.Config;
    using Newtonsoft.Json.Converters;
    using TestExtensions;
    using TestModels;
    using Xunit;
    using Xunit.Abstractions;
    using System.Text.RegularExpressions;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.IIoT.Hub.Models;
    using IIoTPlatform_E2E_Tests.TestEventProcessor;

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

            var client = new RestSharp.RestClient($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token") {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds,
                Authenticator = new HttpBasicAuthenticator(clientId, clientSecret)
            };

            var request = new RestRequest(Method.POST);
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
                            var json = await response.Content.ReadAsStringAsync();
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
        public static IRestResponse CallRestApi(
            IIoTPlatformTestContext context,
            Method method,
            string route,
            object body = null,
            Dictionary<string, string> queryParameters = null,
            CancellationToken ct = default
        ) {
            var accessToken = GetTokenAsync(context, ct).GetAwaiter().GetResult();

            var request = new RestRequest(method);
            request.Resource = route;
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);

            if (body != null) {
                request.AddJsonBody(JsonConvert.SerializeObject(body));
            }

            if (queryParameters != null) {
                foreach (var param in queryParameters) {
                    request.AddQueryParameter(param.Key, param.Value);
                }
            }

            var restClient = new RestSharp.RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };
            var response = restClient.ExecuteAsync(request, ct).GetAwaiter().GetResult();
            return response;
        }

        /// <summary>
        /// Transfer the content of published_nodes.json file into the OPC Publisher edge module
        /// </summary>
        /// <param name="entries">Entries for published_nodes.json</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task PublishNodesAsync(
            IIoTPlatformTestContext context,
            string publishedNodesFullPath,
            IEnumerable<PublishedNodesEntryModel> entries
        ) {
            var json = JsonConvert.SerializeObject(entries, Formatting.Indented);
            return SwitchToStandaloneModeAndPublishNodesAsync(json, context, ct);
        }

        /// <summary>
        /// Transfer the content of published_nodes.json file into the OPC Publisher edge module
        /// </summary>
        /// <param name="json">String for published_nodes.json</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task SwitchToStandaloneModeAndPublishNodesAsync(
            string json,
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        ) {
            context.OutputHelper?.WriteLine("Write published_nodes.json to IoT Edge");
            context.OutputHelper?.WriteLine(json);
            CreateFolderOnEdgeVM(TestConstants.PublishedNodesFolder, context);
            using var scpClient = CreateScpClientAndConnect(context);
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            scpClient.Upload(stream, publishedNodesFullPath);

            if (context.IoTEdgeConfig.NestedEdgeFlag == "Enable") {
                using var sshCient = CreateSshClientAndConnect(context);
                foreach (var edge in context.IoTEdgeConfig.NestedEdgeSshConnections) {
                    if (edge != string.Empty) {
                        // Copy file to the edge vm
                        var command = $"scp -oStrictHostKeyChecking=no {publishedNodesFullPath} {edge}:{TestConstants.PublishedNodesFilename}";
                        sshCient.RunCommand(command);

                        // Move file to the target folder with sudo permissions
                        command = $"ssh -oStrictHostKeyChecking=no {edge} 'sudo mv {TestConstants.PublishedNodesFilename} {publishedNodesFullPath}'";
                        sshCient.RunCommand(command);
                    }
                }
            }
        }

        /// <summary>
        /// Clean published nodes JSON files for both legacy (2.5) and current (2.8) versions.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static async Task CleanPublishedNodesJsonFilesAsync(IIoTPlatformTestContext context) {
            // Make sure directories exist.
            using (var sshCient = CreateSshClientAndConnect(context)) {
                sshCient.RunCommand($"[ ! -d { TestConstants.PublishedNodesFolder} ]" +
                    $" && sudo mkdir -m 777 -p {TestConstants.PublishedNodesFolder}");
                sshCient.RunCommand($"[ ! -d { TestConstants.PublishedNodesFolderLegacy} ]" +
                    $" && sudo mkdir -m 777 -p {TestConstants.PublishedNodesFolderLegacy}");
            }

            await PublishNodesAsync(
                context,
                TestConstants.PublishedNodesFullName,
                Array.Empty<PublishedNodesEntryModel>()
            ).ConfigureAwait(false);

            await PublishNodesAsync(
                context,
                TestConstants.PublishedNodesFullNameLegacy,
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

            context.OutputHelper?.WriteLine("Create SSH Client");
            var client = new SshClient(
                context.SshConfig.Host,
                context.SshConfig.Username,
                privateKeyFile);

            context.OutputHelper?.WriteLine("open ssh connection to host {0} with username {1}",
                context.SshConfig.Host,
                context.SshConfig.Username);
            client.Connect();
            context.OutputHelper?.WriteLine("ssh connection successful established");

            return client;
        }

        /// <summary>
        /// Create a new ScpClient based on SshConfig and directly connects to host
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <returns>Instance of SshClient, that need to be disposed</returns>
        private static ScpClient CreateScpClientAndConnect(IIoTPlatformTestContext context) {
            var privateKeyFile = GetPrivateSshKey(context);

            context.OutputHelper?.WriteLine("Create SCP Client");
            var client = new ScpClient(
                context.SshConfig.Host,
                context.SshConfig.Username,
                privateKeyFile);

            context.OutputHelper?.WriteLine("open scp connection to host {0} with username {1}",
                context.SshConfig.Host,
                context.SshConfig.Username);
            client.Connect();
            context.OutputHelper?.WriteLine("scp connection successful established");

            return client;
        }

        /// <summary>
        /// Gets the private SSH key from the configuration to connect to the Edge VM
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <returns></returns>
        private static PrivateKeyFile GetPrivateSshKey(IIoTPlatformTestContext context)
        {
            context.OutputHelper?.WriteLine("Load private key from environment variable");

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

            var client = new RestSharp.RestClient(runtimeUrl) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds,
                Authenticator = new HttpBasicAuthenticator(context.TestEventProcessorConfig.TestEventProcessorUsername,
                    context.TestEventProcessorConfig.TestEventProcessorPassword)
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

            var request = new RestRequest(Method.PUT);
            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request, ct);
            Assert.True(response.IsSuccessful, $"Response status code: {response.StatusCode}");

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

            var client = new RestSharp.RestClient(runtimeUrl) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds,
                Authenticator = new HttpBasicAuthenticator(context.TestEventProcessorConfig.TestEventProcessorUsername,
                    context.TestEventProcessorConfig.TestEventProcessorPassword)
            };

            var body = new {
                CommandType = CommandEnum.Stop,
            };

            var request = new RestRequest(Method.PUT);
            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request, ct);

            var result = JsonConvert.DeserializeObject<StopResult>(response.Content);
            Assert.NotNull(result);

            return result;
        }

        /// <summary>
        /// Get Json from testeventprocessor after last test run
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Json object</returns>
        public static async Task<dynamic> GetJsonAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        ) {
            var runtimeUrl = context.TestEventProcessorConfig.TestEventProcessorBaseUrl.TrimEnd('/') + "/Runtime/Messages";

            var client = new RestSharp.RestClient(runtimeUrl) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds,
                Authenticator = new HttpBasicAuthenticator(context.TestEventProcessorConfig.TestEventProcessorUsername,
                    context.TestEventProcessorConfig.TestEventProcessorPassword)
            };

            var request = new RestRequest(Method.GET);

            var response = await client.ExecuteAsync(request, ct);

            dynamic json = JsonConvert.DeserializeObject(response.Content);

            Assert.NotNull(json);
            Assert.NotEmpty(json);

            return json;
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
                var client = new RestSharp.RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) {
                    Timeout = TestConstants.DefaultTimeoutInMilliseconds
                };

                while (true) {
                    ct.ThrowIfCancellationRequested();

                    var tasks = new List<Task<IRestResponse>>();

                    foreach (var healthRoute in healthRoutes) {
                        var request = new RestRequest(Method.GET) {
                            Resource = healthRoute
                        };

                        tasks.Add(client.ExecuteAsync(request, ct));
                    }

                    Task.WaitAll(tasks.ToArray());

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
            catch (Exception) {
                context.OutputHelper?.WriteLine("Error: not all API microservices of IIoT " +
                    "platform are in healthy state.");
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
        public static async Task<PublishedNodesEntryModel> CreateSingleNodeModelAsync(IIoTMultipleNodesTestContext context, CancellationToken ct) {
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
            TransportType transportType = TransportType.Amqp_WebSocket_Only
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
        private static async Task<dynamic> GetEndpointInternalAsync(IIoTPlatformTestContext context, CancellationToken ct) {
            var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
            var client = new RestSharp.RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryEndpoints;

            var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                context.OutputHelper?.WriteLine($"StatusCode: {response.StatusCode}");
                context.OutputHelper?.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "GET /registry/v2/endpoints failed!");
            }

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
        /// Serialize a published nodes json file.
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="port">Port of OPC UA server</param>
        /// <param name="writerId">DataSetWriterId to set</param>
        /// <param name="opcEvents">OPC UA events</param>
        public static string PublishedNodesJson(this IIoTStandaloneTestContext context, uint port, string writerId, JArray opcEvents) {
            return JsonConvert.SerializeObject(
                new JArray(
                    context.PlcAciDynamicUrls.Select(host => new JObject(
                        new JProperty("EndpointUrl", $"opc.tcp://{host}:{port}"),
                        new JProperty("UseSecurity", false),
                        new JProperty("DataSetWriterId", writerId),
                        new JProperty("OpcEvents", opcEvents)))
                ), Formatting.Indented);
        }

        /// <summary>
        /// Create an ACI
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="commandLine">Command line for container</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="fileToUpload">File to upload to the container</param>
        /// <param name="numInstances">Number of instances</param>
        public static async Task CreateSimulationContainerAsync(IIoTPlatformTestContext context, List<string> commandLine, CancellationToken cancellationToken, string fileToUpload = null, int numInstances = 1) {
            var azure = await GetAzureContextAsync(context, cancellationToken);

            if (fileToUpload != null) {
                await UploadFileToStorageAccountAsync(context.AzureStorageName, context.AzureStorageKey, fileToUpload);
            }

            context.PlcAciDynamicUrls = await Task.WhenAll(
                Enumerable.Range(0, numInstances)
                    .Select(i =>
                        CreateContainerGroupAsync(azure,
                            context.OpcPlcConfig.ResourceGroupName,
                            $"e2etesting-simulation-aci-{i}-{context.TestingSuffix}-dynamic",
                            context.PLCImage,
                            commandLine[0],
                            commandLine.GetRange(1, commandLine.Count - 1).ToArray(),
                            TestConstants.OpcSimulation.FileShareName,
                            context.AzureStorageName,
                            context.AzureStorageKey)));
        }

        /// <summary>
        /// Upload a file to a storage account
        /// </summary>
        /// <param name="storageAccountName">Name of storage account</param>
        /// <param name="storageAccountKey">Key for storage account</param>
        /// <param name="fileName">File name</param>
        private async static Task UploadFileToStorageAccountAsync(string storageAccountName, string storageAccountKey, string fileName) {
            var cloudStorageAccount = new CloudStorageAccount(new StorageCredentials(storageAccountName, storageAccountKey), true);
            var cloudFileClient = cloudStorageAccount.CreateCloudFileClient();
            var share = cloudFileClient.GetShareReference(TestConstants.OpcSimulation.FileShareName);
            var directory = share.GetRootDirectoryReference();

            Assert.False(fileName.Contains('\\'), "\\ can't be used for file path");

            // if fileName contains '/' we will extract the filename
            string onlyFileName;
            if (fileName.Contains('/')) {
                onlyFileName = fileName.Substring(fileName.LastIndexOf('/') + 1);
            }
            else {
                onlyFileName = fileName;
            }

            var cf = directory.GetFileReference(onlyFileName);
            await cf.UploadFromFileAsync(fileName);
        }

        /// <summary>
        /// Create a container group
        /// </summary>
        /// <param name="azure">Azure context</param>
        /// <param name="resourceGroupName">Resource group name</param>
        /// <param name="containerGroupName">Container group name</param>
        /// <param name="containerImage">Container image</param>
        /// <param name="executable">Starting command line</param>
        /// <param name="commandLine">Additional command line options</param>
        /// <param name="fileShareName">File share name</param>
        /// <param name="storageAccountName">Storage account name</param>
        /// <param name="storageAccountKey">Storage account key</param>
        private static async Task<string> CreateContainerGroupAsync(IAzure azure,
                                      string resourceGroupName,
                                      string containerGroupName,
                                      string containerImage,
                                      string executable,
                                      string[] commandLine,
                                      string fileShareName,
                                      string storageAccountName,
                                      string storageAccountKey) {

            IResourceGroup resGroup = azure.ResourceGroups.GetByName(resourceGroupName);
            Region azureRegion = resGroup.Region;

            var containerGroup = await azure.ContainerGroups.Define(containerGroupName)
                .WithRegion(azureRegion)
                .WithExistingResourceGroup(resourceGroupName)
                .WithLinux()
                .WithPublicImageRegistryOnly()
                .DefineVolume("share")
                    .WithExistingReadWriteAzureFileShare(fileShareName)
                    .WithStorageAccountName(storageAccountName)
                    .WithStorageAccountKey(storageAccountKey)
                    .Attach()
                .DefineContainerInstance(containerGroupName)
                    .WithImage(containerImage)
                    .WithExternalTcpPort(50000)
                    .WithCpuCoreCount(0.5)
                    .WithMemorySizeInGB(0.5)
                    .WithVolumeMountSetting("share", "/app/files")
                    .WithStartingCommandLine(executable, commandLine)
                    .Attach()
                .WithDnsPrefix(containerGroupName)
                .CreateAsync();

            return containerGroup.Fqdn;
        }


        /// <summary>
        /// Delete an ACI
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        public static void DeleteSimulationContainer(IIoTPlatformTestContext context) {
            DeleteSimulationContainerAsync(context).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Delete an ACI
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        public static async Task DeleteSimulationContainerAsync(IIoTPlatformTestContext context) {
            await Task.WhenAll(
            context.PlcAciDynamicUrls
                .Select(url => url.Split(".")[0])
                .Select(n => context.AzureContext.ContainerGroups.DeleteByResourceGroupAsync(context.OpcPlcConfig.ResourceGroupName, n))
            );
        }

        /// <summary>
        /// Get an azure context
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async static Task<IAzure> GetAzureContextAsync(IIoTPlatformTestContext context, CancellationToken cancellationToken) {
            if (context.AzureContext != null) {
                return context.AzureContext;
            }

            var defaultAzureCredential = new DefaultAzureCredential();
            var accessToken = await defaultAzureCredential.GetTokenAsync(new TokenRequestContext(new[] { "https://management.azure.com//.default" }), cancellationToken);
            var tokenCredentials = new TokenCredentials(accessToken.Token);

            IAzure azure;

            if (string.IsNullOrEmpty(context.OpcPlcConfig.SubscriptionId)) {
                azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(new AzureCredentials(tokenCredentials,
                                                        tokenCredentials, context.OpcPlcConfig.TenantId,
                                                        AzureEnvironment.AzureGlobalCloud))
                    .WithDefaultSubscription();
            }
            else {
                azure = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(new AzureCredentials(tokenCredentials,
                                                        tokenCredentials, context.OpcPlcConfig.TenantId,
                                                        AzureEnvironment.AzureGlobalCloud))
                    .WithSubscription(context.OpcPlcConfig.SubscriptionId);
            }

            context.AzureContext = azure;

            var testingSuffix = azure.ResourceGroups.GetByName(context.OpcPlcConfig.ResourceGroupName).Tags[TestConstants.OpcSimulation.TestingResourcesSuffixName];
            context.TestingSuffix = testingSuffix;
            context.AzureStorageName = TestConstants.OpcSimulation.AzureStorageNameWithoutSuffix + testingSuffix;

            var storageAccount = azure.StorageAccounts.GetByResourceGroup(context.OpcPlcConfig.ResourceGroupName, context.AzureStorageName);
            context.AzureStorageKey = storageAccount.GetKeys()[0].Value;

            var firstAciIpAddress = context.OpcPlcConfig.Urls.Split(";")[0];
            var containerGroups = azure.ContainerGroups.ListByResourceGroup(context.OpcPlcConfig.ResourceGroupName).ToList();
            var containerGroup = azure.ContainerGroups.ListByResourceGroup(context.OpcPlcConfig.ResourceGroupName)
                .First(g => g.IPAddress == firstAciIpAddress);
            context.PLCImage = containerGroup.Containers.First().Value.Image;

            return azure;
        }

        /// <summary>
        /// Deserializes the JSON structure contained by the specified <see cref="PartitionEvent"/>
        /// into an instance of the specified type.
        /// </summary>
        /// <param name="partitionEvent">The <see cref="PartitionEvent"/> containing the object.</param>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <returns>The instance of <typeparamref name="T"/> being deserialized.</returns>
        public static T DeserializeJson<T>(this PartitionEvent partitionEvent) {
            using var sr = new StreamReader(partitionEvent.Data.BodyAsStream);
            using var reader = new JsonTextReader(sr);
            return kSerializer.Deserialize<T>(reader);
        }

        /// <summary>
        /// Get an Event Hub consumer
        /// </summary>
        /// <param name="config">Configuration for IoT Hub</param>
        public static EventHubConsumerClient GetEventHubConsumerClient(this IIoTHubConfig config) {
            return new EventHubConsumerClient(
                TestConstants.TestConsumerGroupName,
                config.IoTHubEventHubConnectionString);
        }

        /// <summary>
        ///   Reads events from all partitions of the IoT Hub Event Hubs endpoint as an asynchronous enumerable, allowing events to be iterated as they
        ///   become available on the partition, waiting as necessary should there be no events available.
        ///
        ///   Reading begins at the end of each partition seeing only new events as they are published.
        ///
        ///   Breaks up the batched messages contained in the event, and returns only messages for the provided
        ///   DataSetWriterId.
        ///
        ///   This enumerator may block for an indeterminate amount of time for an <c>await</c> if events are not available on the partition, requiring
        ///   cancellation via the <paramref name="cancellationToken"/> to be requested in order to return control.
        /// </summary>
        /// <param name="consumer">The Event Hubs consumer.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> to be used for iterating over messages.</returns>
        public static IAsyncEnumerable<EventData<T>> ReadMessagesFromWriterIdAsync<T>(this EventHubConsumerClient consumer, string dataSetWriterId, [EnumeratorCancellation] CancellationToken cancellationToken) where T : BaseEventTypePayload
            => ReadMessagesFromWriterIdAsync(consumer, dataSetWriterId, cancellationToken)
                .Select(x =>
                    new EventData<T> {
                        EnqueuedTime = x.enqueuedTime,
                        PublisherId = x.publisherId,
                        Messages = x.messages.ToObject<PubSubMessages<T>>()
                    });

        /// <summary>
        ///   Reads events from all partitions of the IoT Hub Event Hubs endpoint as an asynchronous enumerable, allowing events to be iterated as they
        ///   become available on the partition, waiting as necessary should there be no events available.
        ///
        ///   Reading begins at the end of each partition seeing only new events as they are published.
        ///
        ///   Breaks up the batched messages contained in the event, and returns only messages for the provided
        ///   DataSetWriterId.
        ///
        ///   This enumerator may block for an indeterminate amount of time for an <c>await</c> if events are not available on the partition, requiring
        ///   cancellation via the <paramref name="cancellationToken"/> to be requested in order to return control.
        /// </summary>
        /// <param name="consumer">The Event Hubs consumer.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> to be used for iterating over messages.</returns>
        public static IAsyncEnumerable<PendingAlarmEventData<T>> ReadPendingAlarmMessagesFromWriterIdAsync<T>(this EventHubConsumerClient consumer, string dataSetWriterId, [EnumeratorCancellation] CancellationToken cancellationToken) where T : BaseEventTypePayload {
            return ReadMessagesFromWriterIdAsync(consumer, dataSetWriterId, cancellationToken)
                .Select(x =>
                    new PendingAlarmEventData<T> {
                        IsPayloadCompressed = x.isPayloadCompressed,
                        Messages = x.messages.ToObject<PendingAlarmMessages<T>>()
                    }
                );
        }

        /// <summary>
        ///   Reads events from all partitions of the IoT Hub Event Hubs endpoint as an asynchronous enumerable, allowing events to be iterated as they
        ///   become available on the partition, waiting as necessary should there be no events available.
        ///
        ///   Reading begins at the end of each partition seeing only new events as they are published.
        ///
        ///   Breaks up the batched messages contained in the event, and returns only messages for the provided
        ///   DataSetWriterId.
        ///
        ///   This enumerator may block for an indeterminate amount of time for an <c>await</c> if events are not available on the partition, requiring
        ///   cancellation via the <paramref name="cancellationToken"/> to be requested in order to return control.
        /// </summary>
        /// <param name="consumer">The Event Hubs consumer.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> instance to signal the request to cancel the operation.</param>
        /// <returns>An <see cref="IAsyncEnumerable{JObject}"/> to be used for iterating over messages.</returns>
        public static async IAsyncEnumerable<(DateTime enqueuedTime, string publisherId, JObject messages, bool isPayloadCompressed)> ReadMessagesFromWriterIdAsync(this EventHubConsumerClient consumer, string dataSetWriterId, [EnumeratorCancellation] CancellationToken cancellationToken) {
            var events = consumer.ReadEventsAsync(false, cancellationToken: cancellationToken);
            await foreach (var partitionEvent in events.WithCancellation(cancellationToken)) {
                var enqueuedTime = (DateTime)partitionEvent.Data.SystemProperties[MessageSystemPropertyNames.EnqueuedTime];
                JArray batchedMessages = null;
                bool isPayloadCompressed = (string)partitionEvent.Data.Properties["$$ContentEncoding"] == "gzip";
                if (isPayloadCompressed) {
                    var compressedPayload = Convert.FromBase64String(partitionEvent.Data.EventBody.ToString());
                    using (var input = new MemoryStream(compressedPayload)) {
                        using (var gs = new GZipStream(input, CompressionMode.Decompress)) {
                            using (var textReader = new StreamReader(gs)) {
                                batchedMessages = JsonConvert.DeserializeObject<JArray>(await textReader.ReadToEndAsync());
                            }
                        }
                    }
                }
                else {
                    batchedMessages = partitionEvent.DeserializeJson<JArray>();
                }

                // Expect all messages to be the same
                var messageIds = new HashSet<string>();
                foreach (dynamic message in batchedMessages) {
                    Assert.NotNull(message.MessageId.Value);
                    Assert.True(messageIds.Add(message.MessageId.Value));
                    var publisherId = (string)message.PublisherId.Value;
                    Assert.NotNull(publisherId);
                    Assert.Equal("ua-data", message.MessageType.Value);
                    var innerMessages = (JArray)message.Messages;
                    Assert.True(innerMessages.Any(), "Json doesn't contain any messages");

                    foreach (dynamic innerMessage in innerMessages) {
                        var messageWriterId = (string)innerMessage.DataSetWriterId.Value;
                        if (messageWriterId != dataSetWriterId) {
                            continue;
                        }
                        Assert.Equal(1, innerMessage.MetaDataVersion.MajorVersion.Value);
                        Assert.Equal(0, innerMessage.MetaDataVersion.MinorVersion.Value);

                        yield return (enqueuedTime, publisherId, (JObject)innerMessage.Payload, isPayloadCompressed);
                    }
                }
            }
        }

        /// <summary>
        /// Returns elements from an async-enumerable sequence for a given time duration.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="duration">A time duration during which to return data.</param>
        /// <returns>An async-enumerable sequence that contains the elements from the input sequence for the given time period, starting when the first element is retrieved.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        private static IAsyncEnumerable<TSource> TakeDuring<TSource>(this IAsyncEnumerable<TSource> source, TimeSpan duration) {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            // ReSharper disable once ConvertClosureToMethodGroup
            var sw = new Lazy<Stopwatch>(() => Stopwatch.StartNew());
            return source.TakeWhile(_ => sw.Value.Elapsed < duration);
        }

        /// <summary>
        /// Bypasses elements in an async-enumerable sequence as long as a number of distinct items has not been seen, and then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the source sequence.</typeparam>
        /// <typeparam name="TValue">The type of the elements to be compared for distinct values.</typeparam>
        /// <param name="source">An async-enumerable sequence to return elements from.</param>
        /// <param name="valueFunc">A function to generate the value to test for uniqueness, for each element.</param>
        /// <param name="distinctCountToReach">Number of distinct values for valueFunc output to observe until elements should be passed.</param>
        /// <param name="before">An optional action to execute when the first element is observed.</param>
        /// <param name="after">An optional action to execute when the last bypassed element is observed.</param>
        /// <returns>An async-enumerable sequence that contains the elements from the input sequence starting at the first element in the linear series at which the number of distinct values has been observed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="valueFunc"/> is null.</exception>
        private static IAsyncEnumerable<TSource> SkipUntilDistinctCountReached<TSource, TValue>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TValue> valueFunc,
            int distinctCountToReach,
            Action before = default,
            Action after = default
        ) {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            _ = valueFunc ?? throw new ArgumentNullException(nameof(valueFunc));

            var seenValues = new HashSet<TValue>();
            return source.SkipWhile(m => {
                if (seenValues.Count == 0) {
                    before?.Invoke();
                }

                seenValues.Add(valueFunc(m));
                if (seenValues.Count < distinctCountToReach) {
                    return true;
                }

                after?.Invoke();
                return false;
            });
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
            try {
                var methodInfo = new CloudToDeviceMethod(parameters.Name);
                methodInfo.SetPayloadJson(parameters.JsonPayload);
                var result = await (string.IsNullOrEmpty(moduleId) ?
                     serviceClient.InvokeDeviceMethodAsync(deviceId, methodInfo, ct) :
                     serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInfo, ct));
                return new MethodResultModel {
                    JsonPayload = result.GetPayloadAsJson(),
                    Status = result.Status
                };
            }
            catch (Exception e) {
                PrettyPrintException(e, context.OutputHelper);
                return null;
            }
        }
    }
}