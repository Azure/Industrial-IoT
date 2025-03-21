﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.ResourceManager.ContainerRegistry.Models;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.IotHub;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.IotHub.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Represents and edge gateway that can be accessed from
/// cloud.
/// </summary>
internal sealed record class Gateway
{
    /// <summary>
    /// Stop image
    /// </summary>
    /// <returns></returns>
    public NetcapImage Netcap { get; }

    /// <summary>
    /// Stop storage
    /// </summary>
    /// <returns></returns>
    public NetcapStorage Storage { get; }

    /// <summary>
    /// Create gateway
    /// </summary>
    /// <param name="client"></param>
    /// <param name="logger"></param>
    /// <param name="branch"></param>
    public Gateway(ArmClient client, ILogger logger, string? branch = null)
    {
        _client = client;
        _logger = logger;

        Netcap = new NetcapImage(this, branch, logger);
        Storage = new NetcapStorage(this, logger);
    }

    /// <summary>
    /// Select publisher
    /// </summary>
    /// <param name="subscriptionId"></param>
    /// <param name="netcapMonitored"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async ValueTask<bool> SelectPublisherAsync(string? subscriptionId = null,
        bool netcapMonitored = false, CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            // Stop target publishers in iot hubs
            var deployments = await GetPublisherDeploymentsAsync(subscriptionId,
                netcapMonitored, ct).ToListAsync(ct).ConfigureAwait(false);

            // Select IoT Hub with publisher modules deployed
            if (deployments.Count == 0)
            {
                if (!Extensions.IsRunningInContainer())
                {
                    Console.WriteLine("No publishers found. Check again? [Y/N]");
                    var key = Console.ReadKey();
                    Console.WriteLine();
                    if (key.Key != ConsoleKey.Y)
                    {
                        break;
                    }
                }
                else
                {
                    _logger.LogInformation("No publishers found. Trying again...");
                    await Task.Delay(TimeSpan.FromSeconds(5),
                        ct).ConfigureAwait(false);
                }
                continue;
            }
            var selected = deployments[0];
            if (deployments.Count > 1)
            {
                Console.Clear();
                Console.WriteLine($"Found {deployments.Count} publishers:");

                for (var index = 0; index < deployments.Count; index++)
                {
                    Console.WriteLine($"{index + 1}: {deployments[index]}");
                }
                int i;
                Console.WriteLine("Select publisher index: ");
                while (true)
                {
                    if (int.TryParse(Console.ReadLine(), out i)
                        && i <= deployments.Count && i > 0)
                    {
                        break;
                    }
                }
                Console.WriteLine();
                selected = deployments[i - 1];
            }
            else if (!Extensions.IsRunningInContainer())
            {
                Console.WriteLine("Found 1 publisher.");
                var action = netcapMonitored ? "Detach from" : "Attach to";
                Console.WriteLine($"{action} {selected}? [Y/N]");
                var key = Console.ReadKey();
                Console.WriteLine();
                if (key.Key != ConsoleKey.Y)
                {
                    _logger.LogInformation(
                        "Trying again to find other publishers...");
                    continue;
                }
            }

            _subscription = selected.Subscription;
            _deploymentConfigId = selected.DeploymentConfigId;
            _resourceGroupName = selected.ResourceGroupName;
            _connectionString = selected.ConnectionString;
            _iotHubName = selected.IoTHub.Name;
            _publisher = selected.Publisher;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Stop resource group
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="NetcapException"></exception>
    public async Task<ResourceGroupResource> GetResourceGroupAsync(
        CancellationToken ct = default)
    {
        if (_resourceGroupName == null || _subscription == null)
        {
            throw new NetcapException("Hub not selected");
        }
        return await _subscription.GetResourceGroupAsync(_resourceGroupName,
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Stop storage
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NetcapException"></exception>
    public Storage GetStorage()
    {
        if (_publisher == null || _subscription == null)
        {
            throw new NetcapException("Publisher not selected");
        }
        return new Storage(_publisher.DeviceId, _publisher.Id,
            Storage.ConnectionString, _logger);
    }

    /// <summary>
    /// Deploy netcap module and wait until it is connected
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="NetcapException"></exception>
    public async ValueTask DeployNetcapModuleAsync(CancellationToken ct = default)
    {
        if (_publisher == null || _connectionString == null)
        {
            throw new NetcapException("Publisher not selected");
        }

        // Remove storage
        await GetStorage().DeleteAsync(ct).ConfigureAwait(false);

        // Deploy the module using manifest to device with the chosen publisher
        using var registryManager = RegistryManager.CreateFromConnectionString(
            _connectionString);
        var ncModuleId = _publisher.Id + kPostFix;
        _deploymentConfigId = Extensions.FixUpStorageName(
            _publisher.DeviceId + ncModuleId);
        try
        {
            await registryManager.RemoveConfigurationAsync(_deploymentConfigId,
                ct).ConfigureAwait(false);
        }
        catch (ConfigurationNotFoundException) { }

        var publisherTwin = await registryManager.GetTwinAsync(_publisher.DeviceId,
            _publisher.Id, ct).ConfigureAwait(false);

        var hostname = publisherTwin.GetProperty("__hostname__", desired: false);
        var port = publisherTwin.GetProperty("__port__", desired: false);

        // Stop the create options of the publisher
        var agent = await registryManager.GetTwinAsync(_publisher.DeviceId,
            "$edgeAgent", ct).ConfigureAwait(false);
        if (agent?.Properties.Desired.Contains("modules") == true)
        {
            var publisherDeployment = agent.Properties.Desired["modules"][_publisher.Id];
            if (publisherDeployment != null)
            {
                var settings = publisherDeployment["settings"];
                var options = (string?)settings["createOptions"];
                if (options != null)
                {
                    var createOptions = JObject.Parse(options
                        .Replace("\\\"", "\"", StringComparison.Ordinal));

                    if (createOptions.TryGetValue("Hostname", out var hn) &&
                            hn.Type == JTokenType.String)
                    {
                        hostname = hn.Value<string>();
                    }
                    if (createOptions.TryGetValue("HostConfig", out var cfg) &&
                        cfg is JObject hostConfig &&
                        hostConfig.TryGetValue("PortBindings", out var bd) &&
                        bd is JObject bindings)
                    {
                        // Looks like {\"443/tcp\":[{\"HostPort\":\"8081\"}]}
                        foreach (var pm in bindings)
                        {
                            if (pm.Key.StartsWith("443", StringComparison.Ordinal) &&
                                pm.Value is JArray arr && arr.Count > 0 &&
                                arr[0] is JObject o && o.TryGetValue("HostPort", out var p))
                            {
                                port = p.Value<string>();
                                hostname = "localhost"; // Connect to host port
                            }
                        }
                    }
                }
            }
        }

        await registryManager.AddConfigurationAsync(
            new Configuration(_deploymentConfigId)
            {
                TargetCondition = $"deviceId = '{_publisher.DeviceId}'",
                Content = new ConfigurationContent
                {
                    ModulesContent = CreateSidecarDeployment(_publisher.DeviceId, ncModuleId,
                        _publisher.Id, Netcap.LoginServer, Netcap.Username,
                        Netcap.Password, Netcap.Name, Storage.ConnectionString,
                        publisherTwin.GetProperty("__apikey__", desired: false),
                        publisherTwin.GetProperty("__certificate__", desired: false),
                        publisherTwin.GetProperty("__scheme__", desired: false),
                        publisherTwin.GetProperty("__ip__", desired: false),
                        hostname,
                        port)
                }
            }, ct).ConfigureAwait(false);
        await registryManager.UpdateTwinAsync(_publisher.DeviceId, _publisher.Id,
            new Twin
            {
                Tags = new TwinCollection
                {
                    [kDeploymentTag] = _deploymentConfigId
                }
            }, publisherTwin.ETag, ct).ConfigureAwait(false);

        _logger.LogInformation("Deploying netcap module to {DeviceId}...",
            _publisher.DeviceId);

        // Wait until connected
        var connected = false;
        for (var i = 1; !connected && !ct.IsCancellationRequested; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(i, 10)), ct).ConfigureAwait(false);
            var modules = await registryManager.GetModulesOnDeviceAsync(
                _publisher.DeviceId, ct).ConfigureAwait(false);
            connected = modules.Any(m => m.Id == ncModuleId &&
                m.ConnectionState == DeviceConnectionState.Connected);
        }
        _logger.LogInformation("Netcap module deployed to {DeviceId}.",
            _publisher.DeviceId);
    }

    /// <summary>
    /// Remove deployment
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="NetcapException"></exception>
    public async ValueTask RemoveNetcapModuleAsync(CancellationToken ct = default)
    {
        if (_publisher == null || _connectionString == null || _deploymentConfigId == null)
        {
            throw new NetcapException("Publisher not selected");
        }

        using var registryManager = RegistryManager.CreateFromConnectionString(
            _connectionString);
        var ncModuleId = _publisher.Id + kPostFix;

        await registryManager.UpdateTwinAsync(_publisher.DeviceId, _publisher.Id,
            new Twin
            {
                Tags = new TwinCollection
                {
                    [kDeploymentTag] = null
                }
            }, etag: "*", ct).ConfigureAwait(false);

        await registryManager.RemoveConfigurationAsync(
            _deploymentConfigId, ct).ConfigureAwait(false);

        // Uninstalled
        _deploymentConfigId = null;

        _logger.LogInformation("Removing netcap module from {DeviceId}...",
            _publisher.DeviceId);

        // Wait until netcap is not connected anymore
        var connected = true;
        for (var i = 1; connected && !ct.IsCancellationRequested; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Min(i, 10)), ct).ConfigureAwait(false);
            var modules = await registryManager.GetModulesOnDeviceAsync(
                _publisher.DeviceId, ct).ConfigureAwait(false);
            connected = modules.Any(m => m.Id == ncModuleId);
        }
        _logger.LogInformation("Netcap module removed from {DeviceId}.",
            _publisher.DeviceId);
    }

    /// <summary>
    /// Publisher deployment
    /// </summary>
    /// <param name="Subscription"></param>
    /// <param name="ResourceGroupName"></param>
    /// <param name="IoTHub"></param>
    /// <param name="ConnectionString"></param>
    /// <param name="Publisher"></param>
    /// <param name="Connected"></param>
    /// <param name="DeploymentConfigId"></param>
    internal sealed record class PublisherDeployment(SubscriptionResource Subscription,
        string ResourceGroupName, IotHubDescriptionData IoTHub, string ConnectionString,
        Module Publisher, bool Connected, string? DeploymentConfigId)
    {
        public override string? ToString()
        {
            return $"[{Subscription.Data.DisplayName}/{ResourceGroupName}/" +
                $"{IoTHub.Name}]  {Publisher.DeviceId}/modules/{Publisher.Id}" +
                $"{(Connected ? "" : "  [Disconnected]")}";
        }
    }

    /// <summary>
    /// Stop deployments
    /// </summary>
    /// <param name="subscriptionId"></param>
    /// <param name="netcapMonitored"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async IAsyncEnumerable<PublisherDeployment> GetPublisherDeploymentsAsync(
        string? subscriptionId = null, bool netcapMonitored = false,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogInformation("Finding publishers...");
        await foreach (var sub in _client.GetSubscriptions().GetAllAsync(ct).ConfigureAwait(false))
        {
            if (subscriptionId != null && sub.Data.DisplayName != subscriptionId
                && sub.Id.SubscriptionId != subscriptionId)
            {
                continue;
            }
            await foreach (var hub in sub.GetIotHubDescriptionsAsync(cancellationToken: ct).ConfigureAwait(false))
            {
                Response<SharedAccessSignatureAuthorizationRule> keys;
                try
                {
                    keys = await hub.GetKeysForKeyNameAsync("iothubowner",
                        ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to get keys for hub {Hub}.", hub.Data.Name);
                    continue;
                }
                var cs = IotHubConnectionStringBuilder.Create(hub.Data.Properties.HostName,
                    new ServiceAuthenticationWithSharedAccessPolicyKey("iothubowner",
                        keys.Value.PrimaryKey));
                Debug.Assert(hub.Id.ResourceGroupName != null);
                var publishers = await GetPublishersAsync(sub, hub.Id.ResourceGroupName,
                    hub.Data, cs.ToString(), netcapMonitored, ct)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                foreach (var pub in publishers)
                {
                    yield return pub;
                }
            }
        }

        static async IAsyncEnumerable<PublisherDeployment> GetPublishersAsync(
            SubscriptionResource sub, string resourceGroupName, IotHubDescriptionData hub,
            string connectionString, bool netcapMonitored,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            using var registry = RegistryManager.CreateFromConnectionString(connectionString);
            var query = registry.CreateQuery(
                "SELECT * FROM devices.modules WHERE properties.reported.__type__ = 'OpcPublisher'");
            string? continuationToken = null;
            while (query.HasMoreResults)
            {
                ct.ThrowIfCancellationRequested();
                QueryResponse<Twin> results;
                try
                {
                    results = await query.GetNextAsTwinAsync(new QueryOptions
                    {
                        ContinuationToken = continuationToken
                    }).ConfigureAwait(false);
                }
                catch (UnauthorizedException)
                {
                    yield break;
                }
                continuationToken = results.ContinuationToken;
                foreach (var result in results)
                {
                    var version = result.GetProperty("__version__", desired: false);
                    if (version?.StartsWith("2.9.", StringComparison.Ordinal) != true ||
                        !int.TryParse(version.Split('.')[2], out var patch) ||
                        patch < 10)
                    {
                        // Not supported
                        continue;
                    }

                    var configId = result.GetTag(kDeploymentTag);
                    if (!netcapMonitored)
                    {
                        if (configId != null)
                        {
                            // Select only publishers without netcap enabled
                            continue;
                        }
                        if (result.ConnectionState != DeviceConnectionState.Connected)
                        {
                            // Select only connected publishers
                            continue;
                        }
                    }
                    else if (configId == null)
                    {
                        // Select only publisher with netcap enabled
                        // Disconnected are ok, we want to uninstall those too
                        continue;
                    }

                    var device = await registry.GetDeviceAsync(result.DeviceId,
                        ct).ConfigureAwait(false);
                    var connected = device.ConnectionState == DeviceConnectionState.Connected;
                    yield return new PublisherDeployment(sub, resourceGroupName, hub,
                        connectionString, new Module(result.DeviceId, result.ModuleId),
                        connected, configId);
                }
            }
        }
    }

    /// <summary>
    /// Create Sidecar Deployment deployment
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="netcapModuleId"></param>
    /// <param name="publisherModuleId"></param>
    /// <param name="server"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="image"></param>
    /// <param name="storageConnectionString"></param>
    /// <param name="apiKey"></param>
    /// <param name="certificate"></param>
    /// <param name="scheme"></param>
    /// <param name="ipAddresses"></param>
    /// <param name="hostName"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    private static IDictionary<string, IDictionary<string, object>>? CreateSidecarDeployment(
        string deviceId, string netcapModuleId, string publisherModuleId, string server,
        string userName, string password, string image, string storageConnectionString,
        string? apiKey, string? certificate, string? scheme, string? ipAddresses,
        string? hostName, string? port)
    {
        var args = new List<string>
        {
            "-d", deviceId,
            "-m", publisherModuleId,
            "-s", storageConnectionString
        };
        if (ipAddresses != null)
        {
            args.Add("-i");
            args.Add(ipAddresses);
        }
        if (apiKey != null)
        {
            args.Add("-a");
            args.Add(apiKey);
        }
        if (certificate != null)
        {
            args.Add("-p");
            args.Add(certificate);
        }
        if (hostName != null)
        {
            scheme ??= "https";
            var url = $"{scheme}://{hostName}";
            if (port != null)
            {
                url += $":{port}";
            }

            args.Add("-r");
            args.Add(url);
        }
        var createOptions = JsonConvert.SerializeObject(new
        {
            User = "root",
            Cmd = args.ToArray(),
            NetworkingConfig = new
            {
                EndpointsConfig = new
                {
                    host = new { }
                }
            },
            HostConfig = new
            {
                NetworkMode = "host", // $"container:{hostName ?? publisherModuleId}",
                CapAdd = new[] { "NET_ADMIN" }
            }
        }).Replace("\"", "\\\"", StringComparison.Ordinal);

        // Return deployment modules object
        return JsonConvert.DeserializeObject<IDictionary<string, IDictionary<string, object>>>($$"""
        {
            "$edgeAgent": {
                "properties.desired.runtime.settings.registryCredentials.{{netcapModuleId}}": {
                    "address": "{{server}}",
                    "password": "{{password}}",
                    "username": "{{userName}}"
                },
                "properties.desired.modules.{{netcapModuleId}}": {
                    "settings": {
                        "image": "{{image}}",
                        "createOptions": "{{createOptions}}"
                    },
                    "type": "docker",
                    "status": "running",
                    "restartPolicy": "always",
                    "version": "1.0"
                }
            },
            "$edgeHub": {
                "properties.desired.routes.netcapToUpstream": {
                    "route": "FROM /messages/modules/{{netcapModuleId}}/* INTO $upstream"
                }
            }
        }
""");
    }

    /// <summary>
    /// Create resource name
    /// </summary>
    /// <param name="postfix"></param>
    /// <returns></returns>
    private string GetResourceName(string postfix)
    {
        return Extensions.FixUpResourceName(_iotHubName + postfix);
    }

    /// <summary>
    /// Container image
    /// </summary>
    internal sealed record class NetcapImage
    {
        public string LoginServer { get; private set; } = null!;
        public string Username { get; private set; } = null!;
        public string Password { get; private set; } = null!;
        public string Name { get; private set; } = null!;

        /// <summary>
        /// Create image
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="branch"></param>
        /// <param name="logger"></param>
        public NetcapImage(Gateway gateway, string? branch, ILogger logger)
        {
            _gateway = gateway;
            _branch = branch ?? "main";
            _logger = logger;
        }

        /// <summary>
        /// Create or update
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task CreateOrUpdateAsync(CancellationToken ct = default)
        {
            // Create container registry or update if it already exists in rg
            var regName = _gateway.GetResourceName(kResourceName);
            var rg = await _gateway.GetResourceGroupAsync(ct).ConfigureAwait(false);
            _logger.LogInformation(
                "Create netcap module image in {Registry} inside {ResourceGroup}.",
                regName, rg.Data.Name);
            var registryResponse = await rg.GetContainerRegistries()
                .CreateOrUpdateAsync(WaitUntil.Completed, regName,
                new ContainerRegistryData(rg.Data.Location,
                    new ContainerRegistrySku(ContainerRegistrySkuName.Basic))
                {
                    IsAdminUserEnabled = true,
                    PublicNetworkAccess = ContainerRegistryPublicNetworkAccess.Enabled
                },
                    ct).ConfigureAwait(false);
            var registryKeys = await registryResponse.Value.GetCredentialsAsync(ct)
                .ConfigureAwait(false);

            LoginServer = registryResponse.Value.Data.LoginServer;
            Username = registryKeys.Value.Username;
            Password = registryKeys.Value.Passwords[0].Value;
            Name = LoginServer + "/netcap:" + GetType().Assembly.GetVersion();

            _logger.LogInformation("Building Image {Image} ...", Name);
            // Build the image and push to the registry
            var quickBuild = new ContainerRegistryDockerBuildContent("Dockerfile",
                new ContainerRegistryPlatformProperties("linux") { Architecture = "amd64" })
            {
                SourceLocation =
                    $"https://github.com/Azure/Industrial-IoT.git#{_branch}:samples/Netcap/src",
                IsPushEnabled = true
            };
            quickBuild.ImageNames.Add(Name);
            quickBuild.Arguments.Add(
                new ContainerRegistryRunArgument("VERSION", GetType().Assembly.GetVersion()));
            quickBuild.Arguments.Add(
                new ContainerRegistryRunArgument("BRANCH", _branch));
            var taskName = Extensions.FixUpResourceName(kTaskName + DateTime.UtcNow.ToBinary());
            var buildResponse = await registryResponse.Value.GetContainerRegistryTaskRuns()
                .CreateOrUpdateAsync(WaitUntil.Started, taskName, new ContainerRegistryTaskRunData
                {
                    RunRequest = quickBuild
                }, ct).ConfigureAwait(false);

            var runs = await registryResponse.Value.GetContainerRegistryTaskRuns()
                .GetAsync(taskName, ct).ConfigureAwait(false);
            var run = await registryResponse.Value.GetContainerRegistryRuns().GetAsync(
                 runs.Value.Data.RunResult.RunId, ct).ConfigureAwait(false);

            using var cts = new CancellationTokenSource();
            var copyTask = LogRunLog(run.Value, cts.Token);

            async Task LogRunLog(ContainerRegistryRunResource run, CancellationToken ct)
            {
                var position = 0;
                var url = await run.GetLogSasUrlAsync(ct).ConfigureAwait(false);
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var client = new BlobClient(new Uri(url.Value.LogLink));
#pragma warning disable RCS1261 // Resource can be disposed asynchronously
                        using var os = Console.OpenStandardOutput();
                        using var source = await client.OpenReadAsync(new BlobOpenReadOptions(true)
                        {
                            Position = position
                        }, ct).ConfigureAwait(false);
#pragma warning restore RCS1261 // Resource can be disposed asynchronously
                        position += await source.CopyAsync(os, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Download from {Url} failed.", url.Value.LogLink);
                    }
                }
            }

            await buildResponse.WaitForCompletionAsync(ct).ConfigureAwait(false);
            await cts.CancelAsync().ConfigureAwait(false);
            try { await copyTask.ConfigureAwait(false); } catch { }

            _logger.LogInformation("Image {Image} built with {Result}", Name,
                buildResponse.Value.Data.RunResult.Status.ToString());
        }

        /// <summary>
        /// Create or update
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task DeleteAsync(CancellationToken ct)
        {
            var regName = _gateway.GetResourceName(kResourceName);
            var rg = await _gateway.GetResourceGroupAsync(ct).ConfigureAwait(false);
            var registryCollection = rg.GetContainerRegistries();
            if (!await registryCollection.ExistsAsync(regName, ct).ConfigureAwait(false))
            {
                return;
            }
            var registryResponse = await rg.GetContainerRegistryAsync(regName,
                ct).ConfigureAwait(false);
            await registryResponse.Value.DeleteAsync(WaitUntil.Completed,
                ct).ConfigureAwait(false);

            LoginServer = null!;
            Username = null!;
            Password = null!;
            Name = null!;
        }

        private const string kResourceName = "acr";
        private const string kTaskName = "nc";
        private readonly Gateway _gateway;
        private readonly string _branch;
        private readonly ILogger _logger;
    }

    /// <summary>
    /// NetcapException storage controller
    /// </summary>
    internal sealed record class NetcapStorage
    {
        public string ConnectionString { get; private set; } = null!;

        /// <summary>
        /// Create storage
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="logger"></param>
        public NetcapStorage(Gateway gateway, ILogger logger)
        {
            _gateway = gateway;
            _logger = logger;
        }

        /// <summary>
        /// Create or update
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="NetcapException"></exception>
        public async ValueTask CreateOrUpdateAsync(CancellationToken ct)
        {
            var stgName = _gateway.GetResourceName(kResourceName);
            var rg = await _gateway.GetResourceGroupAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Create Storage {Storage} for netcap module in {Rg}.",
                stgName, rg.Data.Name);
            var storageResponse = await rg.GetStorageAccounts()
                .CreateOrUpdateAsync(WaitUntil.Completed, stgName,
                new StorageAccountCreateOrUpdateContent(
                    new StorageSku(StorageSkuName.StandardGrs),
                    StorageKind.StorageV2, rg.Data.Location)
                {
                    AllowSharedKeyAccess = true,
                    EnableHttpsTrafficOnly = true,
                    PublicNetworkAccess = StoragePublicNetworkAccess.Enabled
                }, ct).ConfigureAwait(false);

            var storageName = storageResponse.Value.Data.Name;
            var endpoints = storageResponse.Value.Data.PrimaryEndpoints;

            var keys = await storageResponse.Value.GetKeysAsync(
                cancellationToken: ct).ToListAsync(ct).ConfigureAwait(false);
            if (keys.Count == 0)
            {
                throw new NetcapException(
                    $"No keys found for storage account {storageName}");
            }

            // Create connection string for storage account
            ConnectionString = "DefaultEndpointsProtocol=https;" +
                $"BlobEndpoint={endpoints.BlobUri};" +
                $"QueueEndpoint={endpoints.QueueUri};" +
                $"FileEndpoint={endpoints.FileUri};" +
                $"TableEndpoint={endpoints.TableUri};" +
                $"AccountName={storageName};AccountKey={keys[0].Value}";
            _logger.LogInformation("Storage {Name} for netcap module created.",
                storageName);
        }

        /// <summary>
        /// Stop
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async ValueTask DeleteAsync(CancellationToken ct)
        {
            var stgName = _gateway.GetResourceName(kResourceName);
            var rg = await _gateway.GetResourceGroupAsync(ct).ConfigureAwait(false);
            var storageCollection = rg.GetStorageAccounts();
            if (!await storageCollection.ExistsAsync(stgName,
                cancellationToken: ct).ConfigureAwait(false))
            {
                return;
            }
            var storageResponse = await rg.GetStorageAccountAsync(stgName,
                cancellationToken: ct).ConfigureAwait(false);
            await storageResponse.Value.DeleteAsync(WaitUntil.Completed,
                ct).ConfigureAwait(false);

            ConnectionString = null!;
        }

        private const string kResourceName = "stg";
        private readonly Gateway _gateway;
        private readonly ILogger _logger;
    }

    private const string kDeploymentTag = "netcapdeployment";
    private const string kPostFix = "-nc";
    private Module? _publisher;
    private SubscriptionResource? _subscription;
    private string? _iotHubName;
    private string? _deploymentConfigId;
    private string? _resourceGroupName;
    private string? _connectionString;
    private readonly ILogger _logger;
    private readonly ArmClient _client;
}
