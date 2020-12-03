// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests {
    using IIoTPlatform_E2E_Tests.Config;
    using Newtonsoft.Json;
    using Renci.SshNet;
    using RestSharp;
    using RestSharp.Authenticators;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;
    using TestModels;
    using Xunit;

    internal static class TestHelper {

        /// <summary>
        /// Request OAuth token using Http basic authentication from environment variables
        /// </summary>
        /// <returns>Return content of request token or empty string</returns>
        public static async Task<string> GetTokenAsync(IIoTPlatformTestContext context) {
            return await GetTokenAsync(
                context.IIoTPlatformConfigHubConfig.AuthTenant,
                context.IIoTPlatformConfigHubConfig.AuthClientId,
                context.IIoTPlatformConfigHubConfig.AuthClientSecret,
                context.IIoTPlatformConfigHubConfig.ApplicationName
            );
        }

        /// <summary>
        /// Request OAuth token using Http basic authentication
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="clientId">User name for HTTP basic authentication</param>
        /// <param name="clientSecret">Password for HTTP basic authentication</param>
        /// <param name="applicationName">Name of deployed Industrial IoT</param>
        /// <returns>Return content of request token or empty string</returns>
        public static async Task<string> GetTokenAsync(string tenantId, string clientId, string clientSecret, string applicationName) {

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

            var response = await client.ExecuteAsync(request);
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
        /// <returns>Dictionary with URL of PLC-PLC as key and Content of Published Nodes files as value</returns>
        public static async Task<IDictionary<string, PublishedNodesEntryModel>> GetSimulatedOpcUaNodesAsync(IIoTPlatformTestContext context) {
            var result = new Dictionary<string, PublishedNodesEntryModel>();

            var opcPlcUrls = context.OpcPlcConfig.Urls;
            context.OutputHelper?.WriteLine($"SimulatedOpcPlcUrls {opcPlcUrls}");
            var listOfUrls = opcPlcUrls.Split(TestConstants.SimulationUrlsSeparator);

            foreach (var url in listOfUrls.Where(s => !string.IsNullOrWhiteSpace(s))) {
                context.OutputHelper?.WriteLine($"Load pn.json from {url}");
                try {
                    using (var client = new HttpClient()) {
                        var ub = new UriBuilder { Host = url };
                        var baseAddress = ub.Uri;

                        client.BaseAddress = baseAddress;
                        client.Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds);

                        using (var response = await client.GetAsync(TestConstants.OpcSimulation.PublishedNodesFile)) {
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
        /// Save PublishedNodes json from OPC-PLC and provide the data to the tests
        /// </summary>
        /// <param name="simulatedOpcServer">Dictionary with URL of PLC-PLC as key and Content of Published Nodes files as value</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        public static void SavePublishedNodesFile(PublishedNodesEntryModel simulatedOpcServer, IIoTPlatformTestContext context) {
            Assert.NotNull(simulatedOpcServer);

            PublishedNodesEntryModel[] model = new PublishedNodesEntryModel[] {simulatedOpcServer};
            var json = JsonConvert.SerializeObject(model, Formatting.Indented);
            context.PublishedNodesFileInternalFolder = Directory.GetCurrentDirectory() + "/published_nodes.json";
            File.WriteAllText(context.PublishedNodesFileInternalFolder, json);
        }

        /// <summary>
        /// Switch to publisher standalone mode
        /// </summary>
        public static void SwitchToStandaloneMode(IIoTPlatformTestContext context) {
            var patch =
                @"{
                    tags: {
                        unmanaged: true
                    }
                }";

            UpdateTagAsync(patch, context).Wait();
        }

        /// <summary>
        /// Switch to publisher orchestrated mode
        /// </summary>
        /// <param name="destinationFilePath">Path of the PublishedNodesFile.json file to be deleted</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        public static void SwitchToOrchestratedMode(string destinationFilePath, IIoTPlatformTestContext context) {
            var patch =
               @"{
                    tags: {
                        unmanaged: null
                    }
                }";

            DeletePublishedNodesFile(destinationFilePath, context);
            UpdateTagAsync(patch, context).Wait();
        }

        /// <summary>
        /// Update Device Twin tag
        /// </summary>
        /// <param name="patch">Name of deployed Industrial IoT</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        private static async Task UpdateTagAsync(string patch, IIoTPlatformTestContext context) {
            var registryManager = context.RegistryHelper.RegistryManager;
            var twin = await registryManager.GetTwinAsync(context.DeviceConfig.DeviceId);
            await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);
        }

        /// <summary>
        /// transfer the content of published_nodes.json file into the OPC Publisher edge module
        /// </summary>
        /// <param name="sourceFilePath">Source file path</param>
        /// <param name="destinationFilePath">Destination file path</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        public static void LoadPublishedNodesFile(string sourceFilePath, string destinationFilePath, IIoTPlatformTestContext context) {
            Assert.True(File.Exists(sourceFilePath), "source file does not exist");

            CreateFolderOnIoTEdge(TestConstants.PublishedNodesFolder, context);
            using var keyFile = new PrivateKeyFile(GetPrivateSshKey(context));
            using var client = new ScpClient(
                context.SshConfig.Host,
                context.SshConfig.Username,
                keyFile);
            client.Connect();

            if (string.IsNullOrEmpty(sourceFilePath)) {
                DeletePublishedNodesFile(destinationFilePath, context);
            }
            using (Stream localFile = File.OpenRead(sourceFilePath)) {
                client.Upload(localFile, destinationFilePath);
            }
        }

        /// <summary>
        /// Get Content of environment variable as memory stream
        /// </summary>
        /// <param name="sshConfig">SSH config</param>
        /// <returns>Memory stream instance</returns>
        private static Stream GetPrivateSshKey(ISshConfig sshConfig) {
            var buffer = Encoding.Default.GetBytes(sshConfig.PrivateKey);
            var stream = new MemoryStream(buffer);
            return stream;
        }

        /// <summary>
        /// Create a new SshClient based on SshConfig and directly connects to host
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <returns>Instance of SshClient, that need to be disposed</returns>
        private static SshClient CreateSshClientAndConnect(IIoTPlatformTestContext context) {
            context.OutputHelper?.WriteLine("Load private key from environment variable");
            var privateKeyStream = GetPrivateSshKey(context.SshConfig);
            var privateKeyFile = new PrivateKeyFile(privateKeyStream);

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
        /// Delete published_nodes.json file into the OPC Publisher edge module
        /// </summary>
        /// <param name="destinationFilePath">Destination file path</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        public static void DeletePublishedNodesFile(string destinationFilePath, IIoTPlatformTestContext context) {
            var isSuccessful = false;
            using var client = CreateSshClientAndConnect(context);

            var terminal = client.RunCommand("rm " + destinationFilePath);
            if (string.IsNullOrEmpty(terminal.Error)) {
                isSuccessful = true;
            }
            Assert.True(isSuccessful, "Delete file was not successful");
        }

        /// <summary>
        /// CreateFolder a folder on IoTEdge to store published_nodes.json file
        /// </summary>
        /// <param name="folderPath">Destination file path</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        private static void CreateFolderOnIoTEdge(string folderPath, IIoTPlatformTestContext context) {
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
        /// <returns></returns>
        public static async Task StartMonitoringIncomingMessages(IIoTPlatformTestContext context,
            int expectedValuesChangesPerTimestamp, int expectedIntervalOfValueChanges, int expectedMaximalDuration) {
            var runtimeUrl = context.TestEventProcessorConfig.TestEventProcessorBaseUrl.TrimEnd('/') + "/Runtime";

            var client = new RestClient(runtimeUrl) {
                Timeout = 30000,
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

            var response = await client.ExecuteAsync(request);
            dynamic json = JsonConvert.DeserializeObject(response.Content);
            Assert.NotNull(json);
        }

        /// <summary>
        /// Stops the monitoring of incoming event to an IoT Hub and returns success/failure.
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <returns></returns>
        public static async Task<dynamic> StopMonitoringIncomingMessages(IIoTPlatformTestContext context) {
            // TODO Merge with Start-Method to avoid code duplication
            var runtimeUrl = context.TestEventProcessorConfig.TestEventProcessorBaseUrl.TrimEnd('/') + "/Runtime";

            var client = new RestClient(runtimeUrl) {
                Timeout = 30000,
                Authenticator = new HttpBasicAuthenticator(context.TestEventProcessorConfig.TestEventProcessorUsername,
                    context.TestEventProcessorConfig.TestEventProcessorPassword)
            };

            var body = new {
                CommandType = 1,
            };

            var request = new RestRequest(Method.PUT);
            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request);

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
            CancellationToken ct
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

                while(true) {
                    ct.ThrowIfCancellationRequested();

                    var tasks = new List<Task<IRestResponse>>();

                    foreach(var healthRoute in healthRoutes) {
                        var request = new RestRequest(Method.GET) {
                            Resource = healthRoute
                        };

                        tasks.Add(client.ExecuteAsync(request, ct));
                    }

                    Task.WaitAll(tasks.ToArray());

                    var healthyServices = tasks
                        .Where(task => task.Result.StatusCode == System.Net.HttpStatusCode.OK)
                        .Where(task => task.Result.Content == healthyState)
                        .Count();

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
        /// <returns>content of GET /registry/v2/application request as dynamic object</returns>
        public static async Task<dynamic> WaitForDiscoveryToBeCompletedAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct) {

            ct.ThrowIfCancellationRequested();

            try {
                dynamic json;
                int numberOfItems;
                do {
                    var accessToken = await TestHelper.GetTokenAsync(context);
                    var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

                    var request = new RestRequest(Method.GET);
                    request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                    request.Resource = TestConstants.APIRoutes.RegistryApplications;

                    var response = await client.ExecuteAsync(request);
                    Assert.NotNull(response);
                    Assert.True(response.IsSuccessful, "GET /registry/v2/application failed!");

                    if (!response.IsSuccessful) {
                        context.OutputHelper?.WriteLine($"StatusCode: {response.StatusCode}");
                        context.OutputHelper?.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                    }

                    Assert.NotEmpty(response.Content);
                    json = JsonConvert.DeserializeObject(response.Content);
                    Assert.NotNull(json);
                    numberOfItems = (int)json.items.Count;

                } while (numberOfItems <= 0);

                return json;
            }
            catch (Exception) {
                context.OutputHelper?.WriteLine("Error: discovery module didn't find OPC UA server in time");
                throw;
            }
        }

        /// <summary>
        /// Wait for first OPC UA endpoint to be activated
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>content of GET /registry/v2/endpoints request as dynamic object</returns>
        public static async Task<dynamic> WaitForEndpointToBeActivatedAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct) {

            var accessToken = await TestHelper.GetTokenAsync(context);
            var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl) { Timeout = TestConstants.DefaultTimeoutInMilliseconds };

            ct.ThrowIfCancellationRequested();
            try {
                dynamic json;
                string activationState;
                do {
                    var request = new RestRequest(Method.GET);
                    request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
                    request.Resource = TestConstants.APIRoutes.RegistryEndpoints;

                    var response = await client.ExecuteAsync(request);
                    Assert.NotNull(response);
                    Assert.True(response.IsSuccessful, "GET /registry/v2/endpoints failed!");

                    if (!response.IsSuccessful) {
                        context.OutputHelper?.WriteLine($"StatusCode: {response.StatusCode}");
                        context.OutputHelper?.WriteLine($"ErrorMessage: {response.ErrorMessage}");
                    }

                    Assert.NotEmpty(response.Content);
                    json = JsonConvert.DeserializeObject(response.Content);
                    Assert.NotNull(json);

                    activationState = (string)json.items[0].activationState;
                    // wait the endpoint to be connected
                    if (activationState == "Activated") {
                        await Task.Delay(TestConstants.DefaultTimeoutInMilliseconds);
                    }
                } while (activationState != "ActivatedAndConnected");

                return json;
            }
            catch (Exception) {
                context.OutputHelper?.WriteLine("Error: OPC UA endpoint couldn't be activated");
                throw;
            }
        }
    }
}
