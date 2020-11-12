// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests {
    using System;
    using Newtonsoft.Json;
    using RestSharp;
    using RestSharp.Authenticators;
    using Xunit;
    using Microsoft.Azure.Devices;
    using System.Threading.Tasks;
    using System.IO;
    using Renci.SshNet;

    internal static class TestHelper {

        /// <summary>
        /// Read the base URL of Industrial IoT Platform from environment variables
        /// </summary>
        /// <returns></returns>
        public static string GetBaseUrl() {
            var baseUrl = Environment.GetEnvironmentVariable("PCS_SERVICE_URL");
            Assert.True(!string.IsNullOrWhiteSpace(baseUrl), "baseUrl is null");
            return baseUrl;
        }

        /// <summary>
        /// Request OAuth token using Http basic authentication from environment variables
        /// </summary>
        /// <returns>Return content of request token or empty string</returns>
        public static string GetToken() {
            return GetToken(
                Environment.GetEnvironmentVariable("PCS_AUTH_TENANT"),
                Environment.GetEnvironmentVariable("PCS_AUTH_CLIENT_APPID"),
                Environment.GetEnvironmentVariable("PCS_AUTH_CLIENT_SECRET"),
                Environment.GetEnvironmentVariable("ApplicationName")
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
        /// Switch to publisher standalone mode
        /// </summary>
        public static void SwitchToStandaloneMode() {
            //var connectionString = Environment.GetEnvironmentVariable("PCS_IOTHUB_CONNSTRING");
            var connectionString = "HostName=iothub-mvhmye.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=wmk5g7ooH34xI9AwXMCZMUaebrsez2GlFc+kpwMXkRA=";
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
            var connectionString = Environment.GetEnvironmentVariable("PCS_IOTHUB_CONNSTRING");
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
            var deviceId = Environment.GetEnvironmentVariable("IOT_EDGE_DEVICE_ID");
            //var deviceId = "linuxgateway0-zoep2ar";
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
            var username = Environment.GetEnvironmentVariable("PCS_SIMULATION_USER");
            var password = Environment.GetEnvironmentVariable("PCS_SIMULATION_PASSWORD");
            var host = Environment.GetEnvironmentVariable("IOT_EDGE_DEVICE_DNS_NAME");
            //var username = "dacol";
            //var password = "Pippero1234";
            //var host = "192.168.100.24";
            Assert.True(!string.IsNullOrWhiteSpace(username), "username string is null");
            Assert.True(!string.IsNullOrWhiteSpace(password), "password string is null");

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
            var username = Environment.GetEnvironmentVariable("PCS_SIMULATION_USER");
            var password = Environment.GetEnvironmentVariable("PCS_SIMULATION_PASSWORD");
            var host = Environment.GetEnvironmentVariable("IOT_EDGE_DEVICE_DNS_NAME");
            //var username = "dacol";
            //var password = "Pippero1234";
            //var host = "192.168.100.24";
            Assert.True(!string.IsNullOrWhiteSpace(username), "username string is null");
            Assert.True(!string.IsNullOrWhiteSpace(password), "password string is null");

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
