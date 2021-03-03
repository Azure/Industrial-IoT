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
    using Newtonsoft.Json.Converters;
    using TestExtensions;
    using TestModels;
    using Xunit;
    using Newtonsoft.Json.Linq;
    using Xunit.Abstractions;

    internal static class TestHelper {

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
                Timeout = TestConstants.DefaultTimeoutInMilliseconds,
                Authenticator = new HttpBasicAuthenticator(clientId, clientSecret)
            };

            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("scope", $"https://{tenantId}/{applicationName}-service/.default");

            var response = await client.ExecuteAsync(request, ct);
            Assert.True(response.IsSuccessful, $"Request OAuth2.0 failed, Status {response.StatusCode}, ErrorMessage: {response.ErrorMessage}");
            dynamic json = JsonConvert.DeserializeObject(response.Content);
            Assert.NotNull(json);
            Assert.NotEmpty(json);
            return $"{json.token_type} {json.access_token}";
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

            var opcPlcUrls = context.OpcPlcConfig.Urls;
            context.OutputHelper?.WriteLine($"SimulatedOpcPlcUrls {opcPlcUrls}");
            var listOfUrls = opcPlcUrls.Split(TestConstants.SimulationUrlsSeparator);

            foreach (var url in listOfUrls.Where(s => !string.IsNullOrWhiteSpace(s))) {
                try {
                    using (var client = new HttpClient()) {
                        var ub = new UriBuilder { Host = url };
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

                            result.Add(url, entryModels[0]);
                        }
                    }
                }
                catch (Exception e) {
                    context.OutputHelper?.WriteLine("Error occurred while downloading Message: {0} skipped: {1}", e.Message, url);
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
        /// transfer the content of published_nodes.json file into the OPC Publisher edge module
        /// </summary>
        /// <param name="sourceFilePath">Source file path</param>
        /// <param name="destinationFilePath">Destination file path</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        public static async Task SwitchToStandaloneModeAndPublishNodesAsync(IEnumerable<PublishedNodesEntryModel> entries, IIoTPlatformTestContext context, CancellationToken ct = default) {
            DeleteFileOnEdgeVM(TestConstants.PublishedNodesFullName, context);

            var json = JsonConvert.SerializeObject(entries, Formatting.Indented);
            context.OutputHelper?.WriteLine("Write published_nodes.json to IoT Edge");
            context.OutputHelper?.WriteLine(json);
            CreateFolderOnEdgeVM(TestConstants.PublishedNodesFolder, context);
            using var client = CreateScpClientAndConnect(context);
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            client.Upload(stream, TestConstants.PublishedNodesFullName);

            await SwitchToStandaloneModeAsync(context, ct);
        }

        /// <summary>
        /// Sets the unmanaged-Tag to "true" to enable Standalone-Mode
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <returns></returns>
        private static async Task SwitchToStandaloneModeAsync(IIoTPlatformTestContext context, CancellationToken ct = default) {
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
        /// <returns></returns>
        public static async Task SwitchToOrchestratedModeAsync(IIoTPlatformTestContext context, CancellationToken ct = default) {
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
                Timeout = TestConstants.DefaultTimeoutInMilliseconds,
                Authenticator = new HttpBasicAuthenticator(context.TestEventProcessorConfig.TestEventProcessorUsername,
                    context.TestEventProcessorConfig.TestEventProcessorPassword)
            };

            var body = new {
                CommandType = 0,
                Configuration = new {
                    IoTHubEventHubEndpointConnectionString = context.IoTHubConfig.IoTHubEventHubConnectionString,
                    StorageConnectionString = context.IoTHubConfig.CheckpointStorageConnectionString,
                    ExpectedValueChangesPerTimestamp = expectedValuesChangesPerTimestamp,
                    ExpectedIntervalOfValueChanges = expectedIntervalOfValueChanges,
                    ExpectedMaximalDuration = expectedMaximalDuration
                }
            };

            var request = new RestRequest(Method.PUT);
            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request, ct);
            dynamic json = JsonConvert.DeserializeObject(response.Content);
            Assert.NotNull(json);
        }

        /// <summary>
        /// Stops the monitoring of incoming event to an IoT Hub and returns success/failure.
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns></returns>
        public static async Task<dynamic> StopMonitoringIncomingMessagesAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        ) {
            // TODO Merge with Start-Method to avoid code duplication
            var runtimeUrl = context.TestEventProcessorConfig.TestEventProcessorBaseUrl.TrimEnd('/') + "/Runtime";

            var client = new RestClient(runtimeUrl) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds,
                Authenticator = new HttpBasicAuthenticator(context.TestEventProcessorConfig.TestEventProcessorUsername,
                    context.TestEventProcessorConfig.TestEventProcessorPassword)
            };

            var body = new {
                CommandType = 1,
            };

            var request = new RestRequest(Method.PUT);
            request.AddJsonBody(body);

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
                var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) {
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
        /// Wait until the OPC UA server is discovered
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="requestedEndpointUrls">List of OPC UA endpoint URLS that need to be activated and connected</param>
        /// <returns>content of GET /registry/v2/application request as dynamic object</returns>
        public static async Task<dynamic> WaitForDiscoveryToBeCompletedAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct = default,
            IEnumerable<string> requestedEndpointUrls = null
        ) {
            ct.ThrowIfCancellationRequested();

            try {
                dynamic json;
                int foundEndpoints = 0;
                int numberOfItems;
                bool shouldExit = false;
                do {
                    foundEndpoints = 0;
                    var accessToken = await TestHelper.GetTokenAsync(context, ct);
                    var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) {
                        Timeout = TestConstants.DefaultTimeoutInMilliseconds
                    };

                    var request = new RestRequest(Method.GET);
                    request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                    request.Resource = TestConstants.APIRoutes.RegistryApplications;

                    var response = await client.ExecuteAsync(request, ct);
                    Assert.NotNull(response);
                    Assert.True(response.IsSuccessful, "GET /registry/v2/application failed!");

                    if (!response.IsSuccessful) {
                        context.OutputHelper?.WriteLine($"StatusCode: {response.StatusCode}");
                        context.OutputHelper?.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                    }

                    Assert.NotEmpty(response.Content);
                    json = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
                    Assert.NotNull(json);
                    numberOfItems = (int)json.items.Count;
                    if (numberOfItems <= 0) {
                        await Task.Delay(TestConstants.DefaultDelayMilliseconds);
                    }
                    else {
                        for (int indexOfOpcApplication = 0; indexOfOpcApplication < numberOfItems; indexOfOpcApplication++) {
                            var endpoint = ((string)json.items[indexOfOpcApplication].discoveryUrls[0]).TrimEnd('/');

                            if(requestedEndpointUrls == null || requestedEndpointUrls.Contains(endpoint)) {
                                foundEndpoints++;
                            }
                        }

                        var expectedNumberOfEndpoints = requestedEndpointUrls != null
                                                        ? requestedEndpointUrls.Count()
                                                        : 1;

                        if (foundEndpoints < expectedNumberOfEndpoints) {
                            await Task.Delay(TestConstants.DefaultDelayMilliseconds);
                        } else {
                            shouldExit = true;
                        }
                    }

                } while (!shouldExit);

                return json;
            }
            catch (Exception e) {
                context.OutputHelper?.WriteLine("Error: discovery module didn't find OPC UA server in time");
                PrettyPrintException(e, context.OutputHelper);
                throw;
            }
        }

        /// <summary>
        /// Wait until the OPC UA endpoint is detected
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="requestedEndpointUrls">List of OPC UA endpoint URLS that need to be activated and connected</param>
        /// <returns>content of GET /registry/v2/endpoints request as dynamic object</returns>
        public static async Task<dynamic> WaitForEndpointDiscoveryToBeCompleted(
            IIoTPlatformTestContext context,
            CancellationToken ct = default,
            IEnumerable<string> requestedEndpointUrls = null) {

            ct.ThrowIfCancellationRequested();

            try {
                dynamic json;
                int foundEndpoints = 0;
                int numberOfItems;
                bool shouldExit = false;
                do {
                    json = await Registry_GetEndpointInternalAsync(context, ct).ConfigureAwait(false);

                    foundEndpoints = 0;

                    Assert.NotNull(json);
                    numberOfItems = (int)json.items.Count;
                    if (numberOfItems <= 0) {
                        await Task.Delay(TestConstants.DefaultDelayMilliseconds);
                    }
                    else {
                        for (int indexOfOpcUaEndpoint = 0; indexOfOpcUaEndpoint < numberOfItems; indexOfOpcUaEndpoint++) {
                            var endpoint = ((string)json.items[indexOfOpcUaEndpoint].registration.endpointUrl).TrimEnd('/');

                            if (requestedEndpointUrls == null || requestedEndpointUrls.Contains(endpoint)) {
                                foundEndpoints++;
                            }
                        }

                        var expectedNumberOfEndpoints = requestedEndpointUrls != null
                                                        ? requestedEndpointUrls.Count()
                                                        : 1;

                        if (foundEndpoints < expectedNumberOfEndpoints) {
                            await Task.Delay(TestConstants.DefaultDelayMilliseconds);
                        }
                        else {
                            shouldExit = true;
                        }
                    }

                } while (!shouldExit);

                return json;
            }
            catch (Exception e) {
                context.OutputHelper?.WriteLine("Error: OPC UA endpoint not found in time");
                PrettyPrintException(e, context.OutputHelper);
                throw;
            }
        }

        /// <summary>
        /// Wait for first OPC UA endpoint to be activated
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="requestedEndpointUrls">List of OPC UA endpoint URLS that need to be activated and connected</param>
        /// <returns>content of GET /registry/v2/endpoints request as dynamic object</returns>
        public static async Task<dynamic> WaitForEndpointToBeActivatedAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct = default,
            IEnumerable<string> requestedEndpointUrls = null) {

            var accessToken = await TestHelper.GetTokenAsync(context, ct);
            var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds
            };

            ct.ThrowIfCancellationRequested();
            try {
                dynamic json;
                var activationStates = new List<string>(10);
                do {
                    activationStates.Clear();
                    var request = new RestRequest(Method.GET);
                    request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                    request.Resource = TestConstants.APIRoutes.RegistryEndpoints;

                    var response = await client.ExecuteAsync(request, ct);
                    Assert.NotNull(response);
                    Assert.True(response.IsSuccessful, "GET /registry/v2/endpoints failed!");

                    if (!response.IsSuccessful) {
                        context.OutputHelper?.WriteLine($"StatusCode: {response.StatusCode}");
                        context.OutputHelper?.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                    }

                    Assert.NotEmpty(response.Content);
                    json = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
                    Assert.NotNull(json);

                    int count = (int)json.items.Count;
                    Assert.NotEqual(0, count);
                    if (requestedEndpointUrls == null) {
                        activationStates.Add((string)json.items[0].activationState);
                    } else {
                        for (int indexOfRequestedOpcServer = 0;
                            indexOfRequestedOpcServer < count;
                            indexOfRequestedOpcServer++) {
                            var endpoint = ((string)json.items[indexOfRequestedOpcServer].registration.endpointUrl).TrimEnd('/');
                            if (requestedEndpointUrls.Contains(endpoint)) {
                                activationStates.Add((string)json.items[indexOfRequestedOpcServer].activationState);
                            }
                        }
                    }

                    // wait the endpoint to be connected
                    if (activationStates.Any(s => s == "Activated")) {
                        await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds, ct);
                    }

                } while (activationStates.All(s => s != "ActivatedAndConnected"));

                return json;
            }
            catch (Exception) {
                context.OutputHelper?.WriteLine("Error: OPC UA endpoint couldn't be activated");
                throw;
            }
        }

        /// <summary>
        /// Equivalent to GetSetOfUniqueNodesAsync
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
        /// <param name="nodeId">Id of the parent node or null to browse the root node</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task<List<(string NodeId, string NodeClass, bool Children)>> Twin_GetBrowseEndpointAsync(
                IIoTPlatformTestContext context,
                string endpointId,
                string nodeId = null,
                CancellationToken ct = default) {

            if (string.IsNullOrEmpty(endpointId)) {
                context.OutputHelper.WriteLine($"{nameof(endpointId)} is null or empty");
                throw new ArgumentNullException(nameof(endpointId));
            }

            var result = new List<(string NodeId, string NodeClass, bool Children)>();
            string continuationToken = null;

            do {
                var browseResult = await Twin_GetBrowseEndpoint_InternalAsync(context, endpointId, nodeId, continuationToken, ct);

                if (browseResult.results.Count > 0) {
                    result.AddRange(browseResult.results);
                }

                continuationToken = browseResult.continuationToken;
            } while (continuationToken != null);

            return result;
        }

        /// <summary>
        /// Equivalent to recursive calling GetSetOfUniqueNodesAsync to get the whole hierarchy of nodes
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
        /// <param name="nodeClass">Class of the node to filter to or null for no filtering</param>
        /// <param name="nodeId">Id of the parent node or null to browse the root node</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task<List<(string NodeId, string NodeClass, bool Children)>> Twin_GetBrowseEndpoint_RecursiveAsync(
                IIoTPlatformTestContext context,
                string endpointId,
                string nodeClass = null,
                string nodeId = null,
                CancellationToken ct = default) {

            if (string.IsNullOrEmpty(endpointId)) {
                context.OutputHelper.WriteLine($"{nameof(endpointId)} is null or empty");
                throw new ArgumentNullException(nameof(endpointId));
            }

            var result = new List<(string NodeId, string NodeClass, bool Children)>();
            var nodes = await Twin_GetBrowseEndpointAsync(context, endpointId, nodeId).ConfigureAwait(false);

            foreach (var node in nodes) {
                if (string.IsNullOrEmpty(nodeClass)
                    || string.Equals(nodeClass, node.NodeClass, StringComparison.OrdinalIgnoreCase)) {
                    result.Add(node);
                }
                if (node.Children) {
                    var childNodes = await Twin_GetBrowseEndpoint_RecursiveAsync(
                        context,
                        endpointId,
                        nodeClass,
                        node.NodeId,
                        ct).ConfigureAwait(false);
                    if (childNodes.Any()) {
                        result.AddRange(childNodes);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Registers a server, the discovery url will be saved in the <paramref name="context"/>
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="discoveryUrl">Discovery URL to register</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task Registry_RegisterServerAsync(
                IIoTPlatformTestContext context,
                string discoveryUrl,
                CancellationToken ct = default) {

            if (string.IsNullOrEmpty(discoveryUrl)) {
                context.OutputHelper.WriteLine($"{nameof(discoveryUrl)} is null or empty");
                throw new ArgumentNullException(nameof(discoveryUrl));
            }

            var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

            var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

            var request = new RestRequest(Method.POST);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryApplications;

            var body = new {
                discoveryUrl = discoveryUrl
            };

            request.AddJsonBody(JsonConvert.SerializeObject(body));

            var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                context.OutputHelper.WriteLine($"StatusCode: {response.StatusCode}");
                context.OutputHelper.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "POST /registry/v2/application failed!");
            }

            context.DiscoveryUrl = discoveryUrl;
        }

        /// <summary>
        /// Gets the application ID associated with the DiscoveryUrl property of <paramref name="context"/>
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task<string> Registry_GetApplicationIdAsync(
                IIoTPlatformTestContext context,
                CancellationToken ct = default) {

            var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

            var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryApplications;

            var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                context.OutputHelper.WriteLine($"StatusCode: {response.StatusCode}");
                context.OutputHelper.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "GET /registry/v2/application failed!");
            }

            dynamic result = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());
            var json = (IDictionary<string, object>)result;

            Assert.True(HasProperty(result, "items"), "GET /registry/v2/application response did not contain items");
            Assert.False(result.items == null, "GET /registry/v2/application response items property is null");

            foreach (var item in result.items) {
                var itemDictionary = (IDictionary<string, object>)item;

                if (!itemDictionary.ContainsKey("discoveryUrls")
                    || !itemDictionary.ContainsKey("applicationId")) {
                    continue;
                }

                var discoveryUrls = (List<object>)item.discoveryUrls;
                var itemUrl = (string)discoveryUrls?.FirstOrDefault(url => IsUrlStringsEqual(url as string, context.DiscoveryUrl));

                if (itemUrl != null) {
                    return item.applicationId;
                }
            }

            return null;
        }

        /// <summary>
        /// Unregisters a server identified by <paramref name="applicationId"/>
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="applicationId">Application ID identifying the server to unregister</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task Registry_UnregisterServerAsync(
                IIoTPlatformTestContext context,
                string applicationId,
                CancellationToken ct = default) {

            if (string.IsNullOrEmpty(applicationId)) {
                context.OutputHelper.WriteLine($"{nameof(applicationId)} is null or empty");
                throw new ArgumentNullException(nameof(applicationId));
            }

            var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

            var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

            var request = new RestRequest(Method.DELETE);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = string.Format(TestConstants.APIRoutes.RegistryApplicationsWithApplicationIdFormat, applicationId);

            var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                context.OutputHelper.WriteLine($"StatusCode: {response.StatusCode}");
                context.OutputHelper.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "DELETE /registry/v2/application/{applicationId} failed!");
            }
        }

        /// <summary>
        /// Activates (and waits for activated and connected state) the endpoint from <paramref name="context"/>
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task Registry_ActivateEndpointAsync(IIoTPlatformTestContext context, CancellationToken ct = default) {
            var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

            Assert.False(string.IsNullOrWhiteSpace(context.OpcUaEndpointId), "Endpoint not set in the test context");

            var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) {
                Timeout = TestConstants.DefaultTimeoutInMilliseconds
            };

            var request = new RestRequest(Method.POST);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = string.Format(TestConstants.APIRoutes.RegistryActivateEndpointsFormat, context.OpcUaEndpointId);

            var response = client.ExecuteAsync(request, ct).GetAwaiter().GetResult();
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                context.OutputHelper.WriteLine($"StatusCode: {response.StatusCode}");
                context.OutputHelper.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "POST /registry/v2/endpoints/{endpointId}/activate failed!");
            }

            Assert.Empty(response.Content);

            while (true) {
                Assert.False(ct.IsCancellationRequested, "Endpoint was not activated within the expected timeout");

                var endpointList = await Registry_GetEndpointsAsync(context, ct).ConfigureAwait(false);
                var endpoint = endpointList.FirstOrDefault(e => string.Equals(e.Id, context.OpcUaEndpointId));

                if (string.Equals(endpoint.ActivationState, TestConstants.StateConstants.ActivatedAndConnected)
                        && string.Equals(endpoint.EndpointState, TestConstants.StateConstants.Ready)) {
                    return;
                }

                context.OutputHelper.WriteLine(string.IsNullOrEmpty(endpoint.Url) ? "Endpoint not found" :
                    $"Endpoint state: {endpoint.EndpointState}, activation: {endpoint.ActivationState}");

                await Task.Delay(TestConstants.DefaultDelayMilliseconds).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets endpoints from registry
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task<List<(string Id, string Url, string ActivationState, string EndpointState)>> Registry_GetEndpointsAsync(
                IIoTPlatformTestContext context,
                CancellationToken ct = default) {
            dynamic json = await Registry_GetEndpointInternalAsync(context, ct).ConfigureAwait(false);

            Assert.True(HasProperty(json, "items"), "GET /registry/v2/endpoints response has no items");
            Assert.False(json.items == null, "GET /registry/v2/endpoints response items property is null");
            Assert.NotEqual(0, json.items.Count);

            var result = new List<(string Id, string Url, string ActivationState, string EndpointState)>();

            foreach (var item in json.items) {
                var id = item.registration.id?.ToString();
                var endpointUrl = item.registration.endpointUrl?.ToString();
                var activationState = item.activationState?.ToString();
                var endpointState = item.endpointState?.ToString();
                result.Add((id, endpointUrl, activationState, endpointState));
            }

            return result;
        }

        /// <summary>
        /// Determines if two strings can be considered the representation of the same URL
        /// </summary>
        /// <param name="url1">URL to compare</param>
        /// <param name="url2">URL to compare to</param>
        public static bool IsUrlStringsEqual(string url1, string url2) =>
            string.Equals(url1?.TrimEnd('/'), url2?.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets endpoints from registry
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        private static async Task<dynamic> Registry_GetEndpointInternalAsync(IIoTPlatformTestContext context, CancellationToken ct) {
            var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
            var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            request.Resource = TestConstants.APIRoutes.RegistryEndpoints;

            var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
            Assert.NotNull(response);

            if (!response.IsSuccessful) {
                context.OutputHelper.WriteLine($"StatusCode: {response.StatusCode}");
                context.OutputHelper.WriteLine($"ErrorMessage: {response.ErrorMessage}");
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
            if (outputHelper == null) return;

            var exception = e;
            while (exception != null) {
                outputHelper.WriteLine(exception.Message);
                outputHelper.WriteLine(exception.StackTrace);
                outputHelper.WriteLine("");
                exception = exception.InnerException;
            }
        }

        /// <summary>
        /// Calls a GET twin browse with the given <paramref name="endpointId"/>
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="endpointId">Id of the endpoint as returned by <see cref="Registry_GetEndpoints(IIoTPlatformTestContext)"/></param>
        /// <param name="nodeId">Id of the parent node or null to browse the root node</param>
        /// <param name="continuationToken">Continuation token from the previous call, or null</param>
        /// <param name="ct">Cancellation token</param>
        private static async Task<(List<(string NodeId, string NodeClass, bool Children)> results, string continuationToken)> Twin_GetBrowseEndpoint_InternalAsync(
                IIoTPlatformTestContext context,
                string endpointId,
                string nodeId = null,
                string continuationToken = null,
                CancellationToken ct = default) {

            var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
            var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

            var request = new RestRequest(Method.GET);
            request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);

            if (continuationToken == null) {
                request.Resource = $"twin/v2/browse/{endpointId}";

                if (!string.IsNullOrEmpty(nodeId)) {
                    request.AddQueryParameter("nodeId", nodeId);
                }
            }
            else {
                request.Resource = $"twin/v2/browse/{endpointId}/next";
                request.AddQueryParameter("continuationToken", continuationToken);
            }

            var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
            
            Assert.NotNull(response);
            if (!response.IsSuccessful) {
                context.OutputHelper.WriteLine($"StatusCode: {response.StatusCode}");
                context.OutputHelper.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                Assert.True(response.IsSuccessful, "GET twin/v2/browse/{endpointId} failed!");
            }

            dynamic json = JsonConvert.DeserializeObject<ExpandoObject>(response.Content, new ExpandoObjectConverter());

            Assert.True(HasProperty(json, "references"), "GET twin/v2/browse/{endpointId} response has no items");
            Assert.False(json.references == null, "GET twin/v2/browse/{endpointId} response references property is null");

            var result = new List<(string NodeId, string NodeClass, bool Children)>();

            foreach (var node in json.references) {
                result.Add(
                    (
                        node.target?.nodeId?.ToString(),
                        node.target?.nodeClass?.ToString(),
                        string.Equals(node.target?.children?.ToString(), "true", StringComparison.OrdinalIgnoreCase)));
            }

            var responseContinuationToken = HasProperty(json, "continuationToken") ? json.continuationToken : null;

            return (results: result, continuationToken: responseContinuationToken);
        }

        /// <summary>
        /// Determines if an ExpandoObject has a property
        /// </summary>
        /// <param name="expandoObject">ExpandoObject to exemine</param>
        /// <param name="propertyName">Name of the property</param>
        private static bool HasProperty(object expandoObject, string propertyName) {
            if (!(expandoObject is IDictionary<string, object> dictionary)) {
                throw new InvalidOperationException("Object is not an ExpandoObject");
            }
            return dictionary.ContainsKey(propertyName);
        }
    }
}
