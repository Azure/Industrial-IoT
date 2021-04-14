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

            var restClient = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };
            var response = restClient.ExecuteAsync(request, ct).GetAwaiter().GetResult();
            return response;
        }

        /// <summary>
        /// transfer the content of published_nodes.json file into the OPC Publisher edge module
        /// </summary>
        /// <param name="entries">Entries for published_nodes.json</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task SwitchToStandaloneModeAndPublishNodesAsync(
            IEnumerable<PublishedNodesEntryModel> entries,
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        ) {
            var json = JsonConvert.SerializeObject(entries, Formatting.Indented);
            context.OutputHelper?.WriteLine("Write published_nodes.json to IoT Edge");
            context.OutputHelper?.WriteLine(json);
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
                        var terminal = sshCient.RunCommand(command);
                        // Move file to the target folder with sudo permissions 
                        command = $"ssh -oStrictHostKeyChecking=no {edge} 'sudo mv {TestConstants.PublishedNodesFilename} {TestConstants.PublishedNodesFullName}'";
                        terminal = sshCient.RunCommand(command);
                    }                 
                }             
            }
            
            await SwitchToStandaloneModeAsync(context, ct);
        }

        /// <summary>
        /// Sets the unmanaged-Tag to "true" to enable Standalone-Mode
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns></returns>
        private static async Task SwitchToStandaloneModeAsync(
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

            var client = new RestClient(runtimeUrl) {
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
                    ExpectedMaximalDuration = expectedMaximalDuration
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
                CommandType = CommandEnum.Stop,
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
        /// Gets endpoints from registry
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        private static async Task<dynamic> GetEndpointInternalAsync(IIoTPlatformTestContext context, CancellationToken ct) {
            var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
            var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

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
            if (outputHelper == null) return;

            var exception = e;
            while (exception != null) {
                outputHelper.WriteLine(exception.Message);
                outputHelper.WriteLine(exception.StackTrace);
                outputHelper.WriteLine("");
                exception = exception.InnerException;
            }
        }
    }
}
