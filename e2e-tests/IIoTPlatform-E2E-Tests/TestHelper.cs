// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using RestSharp;
    using RestSharp.Authenticators;
    using TestModels;
    using Xunit;
    using Microsoft.Azure.Devices;
    using System.IO;
    using Renci.SshNet;

    internal static class TestHelper {

      
        /// Read the base URL of Industrial IoT Platform from environment variables
        /// </summary>
        /// <returns></returns>
        public static string GetBaseUrl() {
            var baseUrl = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_SERVICE_URL);
            Assert.True(!string.IsNullOrWhiteSpace(baseUrl), "baseUrl is null");
            return baseUrl;
        }

        /// <summary>
        /// Request OAuth token using Http basic authentication from environment variables
        /// </summary>
        /// <returns>Return content of request token or empty string</returns>
        public static string GetToken() {
            return GetToken(
                Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_AUTH_TENANT),
                Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_AUTH_CLIENT_APPID),
                Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_AUTH_CLIENT_SECRET),
                Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.ApplicationName)
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
        public static string GetToken(string tenantId, string clientId, string clientSecret, string applicationName) {
            
            Assert.True(!string.IsNullOrWhiteSpace(tenantId), "tenantId is null");
            Assert.True(!string.IsNullOrWhiteSpace(clientId), "clientId is null");
            Assert.True(!string.IsNullOrWhiteSpace(clientSecret), "clientSecret is null");
            Assert.True(!string.IsNullOrWhiteSpace(applicationName), "applicationName is null");

            var client = new RestClient($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token") {
                Timeout = 30000, 
                Authenticator = new HttpBasicAuthenticator(clientId, clientSecret)
            };

            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("scope", $"https://{tenantId}/{applicationName}-service/.default");
            
            var response = client.Execute(request);
            Assert.True(response.IsSuccessful, $"Request OAuth2.0 failed, Status {response.StatusCode}, ErrorMessage: {response.ErrorMessage}");
            dynamic json = JsonConvert.DeserializeObject(response.Content);
            return $"{json.token_type} {json.access_token}";
        }

        /// <summary>
        /// Read PublishedNodes json from OPC-PLC and provide the data to the tests
        /// </summary>
        /// <returns>Dictionary with URL of PLC-PLC as key and Content of Published Nodes files as value</returns>
        public static async Task<IDictionary<string, PublishedNodesEntryModel>> GetSimulatedOpcUaNodes() {
            var result = new Dictionary<string, PublishedNodesEntryModel>();

            var plcUrls = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PLC_SIMULATION_URLS);
            Assert.NotNull(plcUrls);
            
            var listOfUrls = plcUrls.Split(TestConstants.SimulationUrlsSeparator);
            
            foreach (var url in listOfUrls.Where(s => !string.IsNullOrWhiteSpace(s))) {
                using (var client = new HttpClient()) {
                    var ub = new UriBuilder {Host = url};
                    var baseAddress = ub.Uri;

                    client.BaseAddress = baseAddress;

                    using (var response = await client.GetAsync(TestConstants.OpcSimulation.PublishedNodesFile)) {
                        Assert.NotNull(response);
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
            
            return result;
        }

        /// <summary>
        /// Switch to publisher standalone mode
        /// </summary>
        public static void SwitchToStandaloneMode() {
            var connectionString = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_IOTHUB_CONNSTRING);
            Assert.True(!string.IsNullOrWhiteSpace(connectionString), "connection string is null");

            _registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            var patch =
                @"{
                    tags: {
                        unmanaged: null
                    }
                }";

            UpdateTagAsync(patch).Wait();
        }

        /// <summary>
        /// Switch to publisher orchestrated mode
        /// </summary>
        /// /// <param name="destinationFilePath">Path of the PublishedNodesFile.json file to be deleted</param>
        public static void SwitchToOrchestratedMode(string destinationFilePath) {
            var connectionString = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_IOTHUB_CONNSTRING);
            Assert.True(!string.IsNullOrWhiteSpace(connectionString), "connection string is null");

            _registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            var patch =
               @"{
                    tags: {
                        unmanaged: true
                    }
                }";

            DeletePublishedNodesFile(destinationFilePath);
            UpdateTagAsync(patch).Wait();
        }

        /// <summary>
        /// Switch to publisher orchestrated mode
        /// </summary>       
        /// <param name="deviceId">Password for HTTP basic authentication</param>
        /// <param name="patch">Name of deployed Industrial IoT</param
        private static async Task UpdateTagAsync(string patch) {
            var deviceId = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.IOT_EDGE_DEVICE_ID);

            Assert.True(!string.IsNullOrWhiteSpace(deviceId), "deviceId string is null");

            var twin = await _registryManager.GetTwinAsync(deviceId);
            await _registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);
        }

        /// <summary>
        /// transfer the content of published_nodes.json file into the OPC Publisher edge module
        /// </summary>       
        /// <param name="sourceFilePath">Source file path</param>
        /// <param name="destinationFilePath">Destination file path</param
        public static void LoadPublishedNodesFile(string sourceFilePath, string destinationFilePath) {
            var username = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_SIMULATION_USER);
            var password = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_SIMULATION_PASSWORD);
            var host = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.IOT_EDGE_DEVICE_DNS_NAME);

            Assert.True(!string.IsNullOrWhiteSpace(username), "username string is null");
            Assert.True(!string.IsNullOrWhiteSpace(password), "password string is null");
            Assert.True(!string.IsNullOrWhiteSpace(host), "host string is null");

            using (ScpClient client = new ScpClient(host, username, password)) {
                client.Connect();

                if (string.IsNullOrEmpty(sourceFilePath)) {
                    DeletePublishedNodesFile(destinationFilePath);
                }
                using (Stream localFile = File.OpenRead(sourceFilePath)) {
                    client.Upload(localFile, destinationFilePath);
                }
                client.Disconnect();
            }
        }

        /// <summary>
        /// Delete published_nodes.json file into the OPC Publisher edge module
        /// </summary>       
        /// <param name="sourceFilePath">Source file path</param>
        /// <param name="destinationFilePath">Destination file path</param
        public static bool DeletePublishedNodesFile(string destinationFilePath) {
            var username = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_SIMULATION_USER);
            var password = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_SIMULATION_PASSWORD);
            var host = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.  IOT_EDGE_DEVICE_DNS_NAME);

            Assert.True(!string.IsNullOrWhiteSpace(username), "username string is null");
            Assert.True(!string.IsNullOrWhiteSpace(password), "password string is null");
            Assert.True(!string.IsNullOrWhiteSpace(host), "host string is null");

            var isSuccessful = false;
            using (SshClient client = new SshClient(host, username, password)) {
                client.Connect();
                var terminal = client.RunCommand("rm " + destinationFilePath);
                if (string.IsNullOrEmpty(terminal.Error)) {
                    isSuccessful = true;
                }
                client.Disconnect();
            }
            return isSuccessful;
        }

        private static RegistryManager _registryManager;
    }
}
