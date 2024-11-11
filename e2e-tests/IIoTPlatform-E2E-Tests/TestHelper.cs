// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests
{
    using IIoTPlatformE2ETests.Config;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.Messaging.EventHubs.Consumer;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Common.Exceptions;
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
    using Xunit;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public record class MethodResultModel(string JsonPayload, int Status);
    public record class MethodParameterModel
    {
        public string Name { get; set; }
        public string JsonPayload { get; set; }
    }

    internal static partial class TestHelper
    {
        /// <summary>
        /// Request OAuth token using Http basic authentication from environment variables
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Return content of request token or empty string</returns>
        public static async Task<string> GetTokenAsync(
            IIoTPlatformTestContext context,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrWhiteSpace(context.IIoTPlatformConfigHubConfig.AuthClientId))
            {
                return null;
            }
            if (context.Token != null && (DateTime.UtcNow + TimeSpan.FromSeconds(10) < context.TokenExpiration))
            {
                return context.Token;
            }
            var (expiration, token) = await GetTokenAsync(
                context.IIoTPlatformConfigHubConfig.AuthTenant,
                context.IIoTPlatformConfigHubConfig.AuthClientId,
                context.IIoTPlatformConfigHubConfig.AuthClientSecret,
                context.IIoTPlatformConfigHubConfig.AuthServiceId,
                context.OutputHelper,
                ct
            ).ConfigureAwait(false);
            context.Token = token;
            context.TokenExpiration = expiration;
            return token;
        }

        /// <summary>
        /// Request OAuth token using Http basic authentication
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="clientId">User name for HTTP basic authentication</param>
        /// <param name="clientSecret">Password for HTTP basic authentication</param>
        /// <param name="serviceId">service id of deployed Industrial IoT</param>
        /// <param name="outputHelper"></param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Return content of request token or empty string</returns>
        public static async Task<(DateTime Expiration, string Token)> GetTokenAsync(
            string tenantId,
            string clientId,
            string clientSecret,
            string serviceId,
            ITestOutputHelper outputHelper,
            CancellationToken ct = default
        )
        {
            Assert.False(string.IsNullOrWhiteSpace(tenantId));
            Assert.False(string.IsNullOrWhiteSpace(clientId));
            Assert.False(string.IsNullOrWhiteSpace(clientSecret));
            Assert.False(string.IsNullOrWhiteSpace(serviceId));

            Exception saved = new UnauthorizedAccessException();
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    using var client = new RestClient($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
                        client => client.Authenticator = new HttpBasicAuthenticator(clientId, clientSecret));

                    var request = new RestRequest("", Method.Post)
                    {
                        Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                    };
                    request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                    request.AddParameter("grant_type", "client_credentials");
                    request.AddParameter("scope", $"{serviceId}/.default");

                    var response = await client.ExecuteAsync(request, ct).ConfigureAwait(false);
                    Assert.True(response.IsSuccessful, $"Request OAuth2.0 failed, Status {response.StatusCode}, ErrorMessage: {response.ErrorMessage}");
                    dynamic json = JsonConvert.DeserializeObject(response.Content);
                    Assert.NotNull(json);
                    Assert.NotEmpty(json);

                    var expiration = DateTime.UtcNow.AddMinutes(1);
                    try
                    {
                        var seconds = (int)json.expires_in;
                        expiration = DateTime.UtcNow.AddSeconds(seconds);
                        outputHelper?.WriteLine($"Retrieved access token, token expires in {seconds} sec.");
                    }
                    catch { }
                    return (DateTime.UtcNow.AddMinutes(5), $"{json.token_type} {json.access_token}");
                }
                catch (Exception ex)
                {
                    saved = ex;
                    outputHelper?.WriteLine($"Error occurred while requesting token: {ex.Message}");
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
            }
            throw saved;
        }

        /// <summary>
        /// Get urls of the simulated test opc servers
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="useIpAddress">use ip address instead of host name</param>
        /// <returns>List of server urls</returns>
        public static List<string> GetSimulatedOpcServerUrls(
            IIoTPlatformTestContext context, bool useIpAddress = true)
        {
            var items = useIpAddress ? context.OpcPlcConfig.Ips : context.OpcPlcConfig.Urls;
            return items.Split(TestConstants.SimulationUrlsSeparator).Select(host => $"opc.tcp://{host}:50000").ToList();
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
        )
        {
            var result = new Dictionary<string, PublishedNodesEntryModel>();

            var opcPlcList = context.OpcPlcConfig.Urls;
            context.OutputHelper.WriteLine($"SimulatedOpcPlcUrls {opcPlcList}");
            var hostList = opcPlcList.Split(TestConstants.SimulationUrlsSeparator);

            foreach (var host in hostList.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                try
                {
                    var entryModels = await GetPublishedNodesEntryModel(host, ct).ConfigureAwait(false);

                    Assert.NotNull(entryModels);
                    Assert.NotEmpty(entryModels);
                    Assert.NotNull(entryModels[0].OpcNodes);
                    Assert.NotEmpty(entryModels[0].OpcNodes);

                    // Set endpoint url correctly when it's not specified in pn.json ie. replace fqdn with the ip address
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
                    var fqdn = Regex.Match(entryModels[0].EndpointUrl, @"opc.tcp:\/\/([^\}]+):").Groups[1].Value;
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
                    entryModels[0].EndpointUrl = entryModels[0].EndpointUrl.Replace(fqdn, host, StringComparison.Ordinal);

                    result.Add(host, entryModels[0]);
                }
                catch (XunitException e)
                {
                    context.OutputHelper.WriteLine("Error occurred while downloading Message: {0} skipped: {1}", e.Message, host);
                }
            }
            return result;
        }

        private static async Task<PublishedNodesEntryModel[]> GetPublishedNodesEntryModel(string host, CancellationToken ct)
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var ub = new UriBuilder { Host = host };
                        client.BaseAddress = ub.Uri;
                        client.Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds);

                        using (var response = await client.GetAsync(new Uri(TestConstants.OpcSimulation.PublishedNodesFile, UriKind.RelativeOrAbsolute), ct).ConfigureAwait(false))
                        {
                            if (response.StatusCode == (HttpStatusCode)470)
                            {
                                // Firewall denied access, the build VM does not allow access to this ip address port 80
                                // see rules at https://dev.azure.com/mseng/Domino/_git/CloudTest?path=/private/Azure/Ev2/ResourceProvider/Templates/NetworkIsolation.Resources.json&version=GBmaster&_a=contents
                                throw new SocketException((int)SocketError.AccessDenied, host + ":80 is not accessible due to firewall setup of build machine. Fix the build.");
                            }
                            Assert.NotNull(response);
                            Assert.True(response.IsSuccessStatusCode, $"http GET request to load pn.json failed, Status {response.StatusCode}");
                            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                            Assert.NotEmpty(json);
                            return JsonConvert.DeserializeObject<PublishedNodesEntryModel[]>(json);
                        }
                    }
                }
                catch (XunitException) when (attempt < 3)
                {
                    await Task.Delay(2000, ct);
                }
            }
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
        )
        {
            var registryManager = context.RegistryHelper.RegistryManager;
            var twin = await registryManager.GetTwinAsync(context.DeviceConfig.DeviceId, ct).ConfigureAwait(false);
            await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag, ct).ConfigureAwait(false);
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
        public static async Task<RestResponse> CallRestApi(
            IIoTPlatformTestContext context,
            Method method,
            string route,
            object body = null,
            Dictionary<string, string> queryParameters = null,
            CancellationToken ct = default
        )
        {
            var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);

            var request = new RestRequest(route, method)
            {
                Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
            };
            if (accessToken != null)
            {
                request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            }

            if (body != null)
            {
                request.AddJsonBody(body);
            }

            if (queryParameters != null)
            {
                foreach (var param in queryParameters)
                {
                    request.AddQueryParameter(param.Key, param.Value);
                }
            }

            using var restClient = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);
            return await restClient.ExecuteAsync(request, ct).ConfigureAwait(false);
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
        )
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await CreateFolderOnEdgeVMAsync(TestConstants.PublishedNodesFolder, context, ct).ConfigureAwait(false);
                    using var scpClient = await CreateScpClientAndConnectAsync(context, ct).ConfigureAwait(false);
                    await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                    scpClient.Upload(stream, TestConstants.PublishedNodesFullName);

                    return;
                }
                catch (Exception ex) when (attempt < 60)
                {
                    context.OutputHelper.WriteLine($"Failed to write {TestConstants.PublishedNodesFullName} to {TestConstants.PublishedNodesFolder} on host {context.SshConfig.Host} with username {context.SshConfig.Username} ({ex.Message})");
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Transfer the content of published_nodes.json file into the OPC Publisher edge module
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="entries">Entries for published_nodes.json</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task PublishNodesAsync(
            IIoTPlatformTestContext context,
            IEnumerable<PublishedNodesEntryModel> entries,
            CancellationToken ct = default
        )
        {
            var json = JsonConvert.SerializeObject(entries, Formatting.Indented);

            context.OutputHelper.WriteLine("Write published_nodes.json to IoT Edge");
            context.OutputHelper.WriteLine(JsonConvert.SerializeObject(entries));

            await PublishNodesAsync(json, context, ct).ConfigureAwait(false);
            await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct);
        }

        /// <summary>
        /// Clean published nodes JSON files.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task CleanPublishedNodesJsonFilesAsync(IIoTPlatformTestContext context,
            CancellationToken ct = default)
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    // Make sure directories exist.
                    using (var sshCient = await CreateSshClientAndConnectAsync(context, ct).ConfigureAwait(false))
                    {
                        sshCient.RunCommand($"[ ! -d {TestConstants.PublishedNodesFolder} ]" +
                            $" && sudo mkdir -m 777 -p {TestConstants.PublishedNodesFolder}");
                    }
                    break;
                }
                catch (Exception ex) when (attempt < 60)
                {
                    context.OutputHelper.WriteLine($"Failed to create folder on host {context.SshConfig.Host} with username {context.SshConfig.Username} ({ex.Message})");
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
            }

            await PublishNodesAsync(context, Array.Empty<PublishedNodesEntryModel>(), ct).ConfigureAwait(false);
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
        )
        {
            const string patch =
                @"{
                    tags: {
                        unmanaged: true
                    }
                }";

            await UpdateTagAsync(patch, context, ct).ConfigureAwait(false);
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
        )
        {
            const string patch =
                @"{
                    tags: {
                        unmanaged: null
                    }
                }";

            await UpdateTagAsync(patch, context, ct).ConfigureAwait(false);

            await DeleteFileOnEdgeVMAsync(TestConstants.PublishedNodesFullName, context, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a new SshClient based on SshConfig and directly connects to host
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct"></param>
        /// <returns>Instance of SshClient, that need to be disposed</returns>
        private static async Task<SshClient> CreateSshClientAndConnectAsync(IIoTPlatformTestContext context,
            CancellationToken ct = default)
        {
            var privateKeyFile = GetPrivateSshKey(context);
            try
            {
                var client = new SshClient(
                    context.SshConfig.Host,
                    context.SshConfig.Username,
                    privateKeyFile);

                var connectAttempt = 0;
                while (true)
                {
                    try
                    {
                        await client.ConnectAsync(ct);
                        return client;
                    }
                    catch (SocketException) when (++connectAttempt < 10)
                    {
                        await Task.Delay(3000, ct).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                context.OutputHelper.WriteLine($"Failed to open ssh connection to host {context.SshConfig.Host} with username {context.SshConfig.Username} ({ex.Message})");
                throw;
            }
        }

        /// <summary>
        /// Create a new ScpClient based on SshConfig and directly connects to host
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct"></param>
        /// <returns>Instance of SshClient, that need to be disposed</returns>
        private static async Task<ScpClient> CreateScpClientAndConnectAsync(IIoTPlatformTestContext context,
            CancellationToken ct = default)
        {
            var privateKeyFile = GetPrivateSshKey(context);
            try
            {
                var client = new ScpClient(
                    context.SshConfig.Host,
                    context.SshConfig.Username,
                    privateKeyFile);

                var connectAttempt = 0;
                while (true)
                {
                    try
                    {
                        await client.ConnectAsync(ct);
                        return client;
                    }
                    catch (SocketException) when (++connectAttempt < 10)
                    {
                        await Task.Delay(3000, ct).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                context.OutputHelper.WriteLine("Failed to open scp connection to host {0} with username {1} ({2})",
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
        private static PrivateKeyFile GetPrivateSshKey(IIoTPlatformTestContext context)
        {
            var buffer = Encoding.Default.GetBytes(context.SshConfig.PrivateKey);
            var privateKeyStream = new MemoryStream(buffer);

            return new PrivateKeyFile(privateKeyStream);
        }

        /// <summary>
        /// Delete a file on the Edge VM
        /// </summary>
        /// <param name="fileName">Filename of the file to delete</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct"></param>
        public static async Task DeleteFileOnEdgeVMAsync(string fileName, IIoTPlatformTestContext context,
            CancellationToken ct = default)
        {
            var isSuccessful = false;
            using var client = await CreateSshClientAndConnectAsync(context, ct).ConfigureAwait(false);

            var terminal = client.RunCommand("rm " + fileName);

            if (string.IsNullOrEmpty(terminal.Error) ||
                terminal.Error.Contains("no such file", StringComparison.OrdinalIgnoreCase))
            {
                isSuccessful = true;
            }
            Assert.True(isSuccessful, "Delete file was not successful");
        }

        /// <summary>
        /// Create a folder on Edge VM (if not exists)
        /// </summary>
        /// <param name="folderPath">Name of the folder to create.</param>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct"></param>
        private static async Task CreateFolderOnEdgeVMAsync(string folderPath, IIoTPlatformTestContext context,
            CancellationToken ct = default)
        {
            Assert.False(string.IsNullOrWhiteSpace(folderPath));

            var isSuccessful = false;
            using var client = await CreateSshClientAndConnectAsync(context, ct).ConfigureAwait(false);

            var terminal = client.RunCommand("sudo mkdir -p " + folderPath + ";cd " + folderPath + "; sudo chmod 777 " + folderPath);

            if (string.IsNullOrEmpty(terminal.Error) || terminal.Error.Contains("File exists", StringComparison.Ordinal))
            {
                isSuccessful = true;
            }

            Assert.True(isSuccessful, $"Folder creation was not successful because of {terminal.Error}");
        }

        /// <summary>
        /// Get an Event Hub consumer
        /// </summary>
        /// <param name="config">Configuration for IoT Hub</param>
        /// <param name="consumerGroup"></param>
        public static EventHubConsumerClient GetEventHubConsumerClient(this IIoTHubConfig config, string consumerGroup = null)
        {
            return new EventHubConsumerClient(
                consumerGroup ?? TestConstants.TestConsumerGroupName,
                config.IoTHubEventHubConnectionString);
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
        )
        {
            const string healthyState = "Healthy";

            var healthRoutes = new string[] {
                TestConstants.APIRoutes.HealthZ
            };

            try
            {
                using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    var tasks = new List<Task<RestResponse>>();

                    foreach (var healthRoute in healthRoutes)
                    {
                        var request = new RestRequest(healthRoute, Method.Get)
                        {
                            Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
                        };

                        tasks.Add(client.ExecuteAsync(request, ct));
                    }

                    await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);

                    var healthyServices = tasks
                        .Where(task => task.Result.StatusCode == HttpStatusCode.OK)
                        .Count(task => task.Result.Content == healthyState);

                    if (healthyServices == healthRoutes.Length)
                    {
                        context.OutputHelper.WriteLine("All API microservices of IIoT platform " +
                            "are running and in healthy state.");
                        return;
                    }

                    await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                context.OutputHelper.WriteLine("Error: not all API microservices of IIoT " +
                    $"platform are in healthy state ({e.Message}).");
                throw;
            }
        }

        /// <summary>
        /// Determines if two strings can be considered the representation of the same URL
        /// </summary>
        /// <param name="url1">URL to compare</param>
        /// <param name="url2">URL to compare to</param>
        public static bool IsUrlStringsEqual(string url1, string url2)
        {
            return string.Equals(url1?.TrimEnd('/'), url2?.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if an ExpandoObject has a property
        /// </summary>
        /// <param name="expandoObject">ExpandoObject to exemine</param>
        /// <param name="propertyName">Name of the property</param>
        /// <exception cref="InvalidOperationException"></exception>
        public static bool HasProperty(object expandoObject, string propertyName)
        {
            if (expandoObject is not IDictionary<string, object> dictionary)
            {
                throw new InvalidOperationException("Object is not an ExpandoObject");
            }
            return dictionary.ContainsKey(propertyName);
        }

        /// <summary>
        /// Create a single node model with opcplc node
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <param name="dataChangeTrigger"></param>
        public static async Task<PublishedNodesEntryModel> CreateSingleNodeModelAsync(IIoTMultipleNodesTestContext context, CancellationToken ct, DataChangeTriggerType? dataChangeTrigger = null)
        {
            IDictionary<string, PublishedNodesEntryModel> simulatedPublishedNodesConfiguration =
                new Dictionary<string, PublishedNodesEntryModel>(0);

            simulatedPublishedNodesConfiguration =
                await GetSimulatedPublishedNodesConfigurationAsync(context, ct).ConfigureAwait(false);

            PublishedNodesEntryModel model;
            if (simulatedPublishedNodesConfiguration.Count > 0)
            {
                model = simulatedPublishedNodesConfiguration[simulatedPublishedNodesConfiguration.Keys.First()];
            }
            else
            {
                var opcPlcIp = context.OpcPlcConfig.Urls.Split(TestConstants.SimulationUrlsSeparator)[0];
                model = new PublishedNodesEntryModel
                {
                    EndpointUrl = $"opc.tcp://{opcPlcIp}:50000",
                    UseSecurity = true,
                    OpcNodes = new List<OpcNodeModel> {
                        new() {
                            Id = "ns=2;s=SlowUInt1",
                            OpcPublishingInterval = 10000
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
                .Where(opcNode => opcNode.Id.Contains("SlowUInt", StringComparison.Ordinal))
                .Take(1).Select(opcNode =>
                {
                    var opcPlcPublishingInterval = opcNode.OpcPublishingInterval;
                    opcNode.OpcPublishingInterval = opcPlcPublishingInterval / 2;
                    opcNode.OpcSamplingInterval = opcPlcPublishingInterval / 4;
                    opcNode.QueueSize = 4;
                    opcNode.DataChangeTrigger = dataChangeTrigger;
                    return opcNode;
                })
                .ToList();

            return model;
        }

        /// <summary>
        /// Create a multiple nodes model with opcplc nodes
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <param name="endpointIndex"></param>
        /// <param name="numberOfNodes"></param>
        public static async Task<PublishedNodesEntryModel> CreateMultipleNodesModelAsync(
            IIoTMultipleNodesTestContext context,
            CancellationToken ct,
            int endpointIndex = 2,
            int numberOfNodes = 250)
        {
            await context.LoadSimulatedPublishedNodesAsync(ct).ConfigureAwait(false);

            PublishedNodesEntryModel nodesToPublish;
            if (context.SimulatedPublishedNodes.Count > endpointIndex)
            {
                var testPlc = context.SimulatedPublishedNodes.Skip(endpointIndex).First().Value;
                nodesToPublish = IIoTMultipleNodesTestContext.GetEntryModelWithoutNodes(testPlc);

                // We want to take several slow and fast nodes.
                // To make sure that we will not have missing values because of timing issues,
                // we will set publishing and sampling intervals to a lower value than the publishing
                // interval of the simulated OPC PLC. This will eliminate false-positives.
                nodesToPublish.OpcNodes = testPlc.OpcNodes
                    .Where(node => !node.Id.Contains("bad", StringComparison.OrdinalIgnoreCase))
                    .Where(node => node.Id.Contains("slow", StringComparison.OrdinalIgnoreCase)
                        || node.Id.Contains("fast", StringComparison.OrdinalIgnoreCase))
                    .Take(numberOfNodes)
                    .Select(opcNode =>
                    {
                        var opcPlcPublishingInterval = opcNode.OpcPublishingInterval;
                        opcNode.OpcPublishingInterval = opcPlcPublishingInterval / 2;
                        opcNode.OpcSamplingInterval = opcPlcPublishingInterval / 4;
                        opcNode.QueueSize = 4;
                        return opcNode;
                    })
                    .ToList();

                context.ConsumedOpcUaNodes.AddOrUpdate(testPlc.EndpointUrl, nodesToPublish);
            }
            else
            {
                var opcPlcIp = context.OpcPlcConfig.Urls.Split(TestConstants.SimulationUrlsSeparator)[endpointIndex];
                nodesToPublish = new PublishedNodesEntryModel
                {
                    EndpointUrl = $"opc.tcp://{opcPlcIp}:50000",
                    UseSecurity = true
                };

                var nodes = new List<OpcNodeModel>();
                for (var i = 0; i < numberOfNodes; i++)
                {
                    nodes.Add(new OpcNodeModel
                    {
                        Id = $"ns=2;s=SlowUInt{i + 1}",
                        OpcPublishingInterval = 10000 / 2,
                        OpcSamplingInterval = 10000 / 4,
                        QueueSize = 4
                    });
                }

                nodesToPublish.OpcNodes = nodes.ToList();
                context.ConsumedOpcUaNodes.Add(opcPlcIp, nodesToPublish);
            }

            return nodesToPublish;
        }

        /// <summary>
        /// Initialize DeviceServiceClient from IoT Hub connection string.
        /// </summary>
        /// <param name="iotHubConnectionString"></param>
        /// <param name="transportType"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static ServiceClient DeviceServiceClient(
            string iotHubConnectionString,
            TransportType transportType = TransportType.Amqp_WebSocket_Only
        )
        {
            if (string.IsNullOrWhiteSpace(iotHubConnectionString))
            {
                throw new ArgumentNullException(nameof(iotHubConnectionString));
            }

            return ServiceClient.CreateFromConnectionString(
                iotHubConnectionString,
                transportType
            );
        }

        /// <summary>
        /// Restart module
        /// </summary>
        /// <param name="context"></param>
        /// <param name="moduleName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task RestartAsync(IIoTPlatformTestContext context, string moduleName,
            CancellationToken ct)
        {
            using var client = TestHelper.DeviceServiceClient(context.IoTHubConfig.IoTHubConnectionString,
                    TransportType.Amqp_WebSocket_Only);
            var method = new CloudToDeviceMethod("Shutdown");
            method.SetPayloadJson("false");
            try
            {
                await client.InvokeDeviceMethodAsync(context.DeviceId, moduleName, method, ct);
            }
            catch { } // Expected, since device will have disconnected now
        }

        /// <summary>
        /// Gets endpoints from registry
        /// </summary>
        /// <param name="context">Shared Context for E2E testing Industrial IoT Platform</param>
        /// <param name="ct">Cancellation token</param>
        private static async Task<dynamic> GetEndpointsInternalAsync(IIoTPlatformTestContext context, CancellationToken ct)
        {
            var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
            using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

            var request = new RestRequest(TestConstants.APIRoutes.RegistryEndpoints, Method.Get)
            {
                Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
            };
            if (accessToken != null)
            {
                request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            }

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
        private static async Task<dynamic> GetApplicationsInternalAsync(IIoTPlatformTestContext context, CancellationToken ct)
        {
            var accessToken = await GetTokenAsync(context, ct).ConfigureAwait(false);
            using var client = new RestClient(context.IIoTPlatformConfigHubConfig.BaseUrl);

            var request = new RestRequest(TestConstants.APIRoutes.RegistryApplications, Method.Get)
            {
                Timeout = TimeSpan.FromMilliseconds(TestConstants.DefaultTimeoutInMilliseconds)
            };
            if (accessToken != null)
            {
                request.AddHeader(TestConstants.HttpHeaderNames.Authorization, accessToken);
            }

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
        private static void PrettyPrintException(Exception e, ITestOutputHelper outputHelper)
        {
            var exception = e;
            while (exception != null)
            {
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
            MethodParameterModel parameters, IIoTPlatformTestContext context, CancellationToken ct)
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    var i = 0;// Retry twice to call with error 500
                    while (true)
                    {
                        var methodInfo = new CloudToDeviceMethod(parameters.Name);
                        methodInfo.SetPayloadJson(parameters.JsonPayload);
                        var result = await (string.IsNullOrEmpty(moduleId) ?
                             serviceClient.InvokeDeviceMethodAsync(deviceId, methodInfo, ct) :
                             serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, methodInfo, ct)).ConfigureAwait(false);
                        context.OutputHelper.WriteLine($"Called method {parameters.Name}.");
                        var methodCallResult = new MethodResultModel(result.GetPayloadAsJson(), result.Status);
                        if (methodCallResult.Status >= 500 && ++i < 3)
                        {
                            context.OutputHelper.WriteLine($"Got internal error {methodCallResult.Status} ({methodCallResult.JsonPayload}), trying again to call publisher after delay...");
                            await Task.Delay(2000, ct).ConfigureAwait(false);
                            continue;
                        }
                        return methodCallResult;
                    }
                }
                catch (DeviceNotFoundException de) when (attempt < 60)
                {
                    context.OutputHelper.WriteLine($"Failed to call method {parameters.Name} with {parameters.JsonPayload} due to {de.Message}");
                    await Task.Delay(TestConstants.DefaultDelayMilliseconds, ct).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    context.OutputHelper.WriteLine($"Failed to call method {parameters.Name} with {parameters.JsonPayload}");
                    if (e.Message.Contains("The operation failed because the requested device isn't online", StringComparison.Ordinal) && attempt < 60)
                    {
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
