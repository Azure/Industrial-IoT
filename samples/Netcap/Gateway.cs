// ------------------------------------------------------------
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
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Azure.ResourceManager.IotHub.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage;

/// <summary>
/// Represents and edge gateway that can be accessed from
/// cloud.
/// </summary>
internal sealed record class Gateway
{
    /// <summary>
    /// Get image
    /// </summary>
    /// <returns></returns>
    public NetcapImage Netcap { get; }

    /// <summary>
    /// Get storage
    /// </summary>
    /// <returns></returns>
    public NetcapStorage Storage { get; }

    /// <summary>
    /// Create gateway
    /// </summary>
    /// <param name="client"></param>
    /// <param name="logger"></param>
    public Gateway(ArmClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;

        Netcap = new NetcapImage(this, logger);
        Storage = new NetcapStorage(this, logger);
    }

    /// <summary>
    /// Select publisher
    /// </summary>
    /// <param name="subscriptionId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async ValueTask SelectPublisherAsync(string? subscriptionId = null,
        CancellationToken ct = default)
    {
        // Get publishers in iot hubs
        var deployments = await GetPublisherDeploymentsAsync(subscriptionId, ct)
            .ToListAsync(ct).ConfigureAwait(false);

        // Select IoT Hub with publisher modules deployed
        if (deployments.Count == 0)
        {
            throw new InvalidOperationException("No publishers found");
        }
        var selected = deployments[0];
        if (deployments.Count > 1)
        {
            Console.Clear();
            for (var index = 0; index < deployments.Count; index++)
            {
                Console.WriteLine($"{index + 1}: {deployments[index]}");
            }
            var i = 0;
            Console.WriteLine("Select index: ");
            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out i)
                    && i <= deployments.Count && i > 0)
                {
                    break;
                }
            }
            selected = deployments[i - 1];
        }

        _subscription = selected.Subscription;
        _resourceGroupName = selected.ResourceGroupName;
        _connectionString = selected.ConnectionString;
        _publisher = selected.Publisher;
    }

    /// <summary>
    /// Get resource group
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<ResourceGroupResource> GetResourceGroupAsync(
        CancellationToken ct = default)
    {
        if (_resourceGroupName == null || _subscription == null)
        {
            throw new InvalidOperationException("Hub not selected");
        }
        return await _subscription.GetResourceGroupAsync(_resourceGroupName,
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Deploy netcap module and wait until it is connected
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async ValueTask DeployNetcapModuleAsync(CancellationToken ct = default)
    {
        if (_publisher == null || _connectionString == null)
        {
            throw new InvalidOperationException("Publisher not selected");
        }

        // Deploy the module using manifest to device with the chosen publisher
        using var registryManager = RegistryManager.CreateFromConnectionString(
            _connectionString);
        var ncModuleId = _publisher.Id + "-nc";
        var configId = _publisher.DeviceId + ncModuleId;
        try { await registryManager.RemoveConfigurationAsync(configId,
                ct).ConfigureAwait(false); }
        catch (ConfigurationNotFoundException) { }
        await registryManager.AddConfigurationAsync(new Configuration(configId)
        {
            TargetCondition = $"deviceId='{_publisher.DeviceId}'",
            Content = new ConfigurationContent
            {
                ModulesContent = Create(_publisher.DeviceId,
                    ncModuleId, _publisher.Id, Netcap.LoginServer, Netcap.Username,
                    Netcap.Password, Netcap.Name, Storage.ConnectionString)
            }
        }, ct).ConfigureAwait(false);
        _logger.LogInformation("Deploying netcap module to {DeviceId}...",
            _publisher.DeviceId);

        // Wait until connected
        var connected = false;
        for (var i = 1; !connected && !ct.IsCancellationRequested; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(i), ct).ConfigureAwait(false);
            var modules = await registryManager.GetModulesOnDeviceAsync(
                _publisher.DeviceId, ct).ConfigureAwait(false);
            connected = modules.Any(m => m.Id == ncModuleId &&
                m.ConnectionState == DeviceConnectionState.Connected);
        }
        _logger.LogInformation("Netcap module created on {DeviceId}.",
            _publisher.DeviceId);
    }

    /// <summary>
    /// Remove deployment
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async ValueTask RemoveNetcapModuleAsync(CancellationToken ct = default)
    {
        if (_publisher == null || _connectionString == null)
        {
            throw new InvalidOperationException("Publisher not selected");
        }

        // Deploy the module using manifest to device with the chosen publisher
        using var registryManager = RegistryManager.CreateFromConnectionString(
            _connectionString);
        var ncModuleId = _publisher.Id + "-nc";
        var configId = _publisher.DeviceId + ncModuleId;
        await registryManager.RemoveConfigurationAsync(configId,
            ct).ConfigureAwait(false);

        // Wait until connected
        var connected = true;
        for (var i = 1; connected && !ct.IsCancellationRequested; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(i), ct).ConfigureAwait(false);
            var modules = await registryManager.GetModulesOnDeviceAsync(
                _publisher.DeviceId, ct).ConfigureAwait(false);
            connected = modules.Any(m => m.Id == ncModuleId &&
                m.ConnectionState == DeviceConnectionState.Connected);
        }
        _logger.LogInformation("Netcap module removed from {DeviceId}.",
            _publisher.DeviceId);
    }

    /// <summary>
    /// Publisher deployment
    /// </summary>
    /// <param name="Subscription"></param>
    /// <param name="ResourceGroupName"></param>
    /// <param name="Hub"></param>
    /// <param name="ConnectionString"></param>
    /// <param name="Publisher"></param>
    internal sealed record class PublisherDeployment(SubscriptionResource Subscription,
        string ResourceGroupName, IotHubDescriptionData Hub,
        string ConnectionString, Module Publisher)
    {
        public override string? ToString()
        {
            return $"[{Subscription.Data.DisplayName}-{ResourceGroupName}] " +
                $"{Hub.Name}: {Publisher.DeviceId}|{Publisher.Id}";
        }
    }

    /// <summary>
    /// Get deployments
    /// </summary>
    /// <param name="subscriptionId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async IAsyncEnumerable<PublisherDeployment> GetPublisherDeploymentsAsync(
        string? subscriptionId = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogInformation("Getting all publishers...");
        await foreach (var sub in _client.GetSubscriptions().GetAllAsync(ct))
        {
            if (subscriptionId != null && sub.Data.DisplayName != subscriptionId
                && sub.Id.SubscriptionId != subscriptionId)
            {
                Console.WriteLine(sub.Data.DisplayName);
                continue;
            }
            await foreach (var hub in sub.GetIotHubDescriptionsAsync(cancellationToken: ct))
            {
                Response<SharedAccessSignatureAuthorizationRule> keys;
                try
                {
                    keys = await hub.GetKeysForKeyNameAsync("iothubowner",
                        ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to get keys for hub {Hub}.",
                        hub.Data.Name);
                    continue;
                }
                var cs = IotHubConnectionStringBuilder.Create(
                    hub.Data.Properties.HostName,
                    new ServiceAuthenticationWithSharedAccessPolicyKey(
                        "iothubowner", keys.Value.PrimaryKey));
                Debug.Assert(hub.Id.ResourceGroupName != null);
                var publishers = await GetPublishersAsync(sub, hub.Id.ResourceGroupName,
                    hub.Data, cs.ToString(), ct)
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
            string connectionString, [EnumeratorCancellation] CancellationToken ct = default)
        {
            using var registry = RegistryManager.CreateFromConnectionString(connectionString);
            var publishers = registry.CreateQuery("SELECT * FROM devices.modules WHERE " +
                "properties.reported.__type__ = 'OpcPublisher'");
            string? continuationToken = null;
            while (publishers.HasMoreResults)
            {
                ct.ThrowIfCancellationRequested();
                QueryResponse<Twin> results;
                try
                {
                    results = await publishers.GetNextAsTwinAsync(new QueryOptions
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

                    if (result.ConnectionState != DeviceConnectionState.Connected)
                    {
                        // Not connected
                        continue;
                    }

                    var device = await registry.GetDeviceAsync(result.DeviceId,
                        ct).ConfigureAwait(false);
                    if (device.ConnectionState != DeviceConnectionState.Connected)
                    {
                        // Not connected
                        continue;
                    }

                    yield return new PublisherDeployment(sub, resourceGroupName, hub,
                        connectionString, new Module(result.DeviceId, result.ModuleId));
                }
            }
        }
    }

    /// <summary>
    /// Create deployment
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="netcapModuleId"></param>
    /// <param name="publisherModuleId"></param>
    /// <param name="server"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="image"></param>
    /// <param name="storageConnectionString"></param>
    /// <returns></returns>
    private static IDictionary<string, IDictionary<string, object>>? Create(string deviceId,
        string netcapModuleId, string publisherModuleId, string server,
        string userName, string password, string image, string storageConnectionString)
    {
        var createOptions = JsonConvert.SerializeObject(new
        {
            Hostname = "netcap",
            User = "root",
            Cmd = new[] {
                "-d", deviceId,
                "-m", publisherModuleId,
                "-s", storageConnectionString
            },
            HostConfig = new
            {
                CapAdd = new[] { "NET_ADMIN" }
            }
        }).Replace("\"", "\\\"", StringComparison.Ordinal);

        // Return deployment modules object
        return JsonConvert.DeserializeObject<IDictionary<string, IDictionary<string, object>>>("""

            {
                "$edgeAgent": {
                    "properties.desired.runtime.settings.registryCredentials.
""" + netcapModuleId + """
": {
                        "address": "
""" + server + """
",
                        "password": "
""" + password + """
",
                        "username": "
""" + userName + """
"
                    },
                    "properties.desired.modules.
""" + netcapModuleId + """
": {
                        "settings": {
                            "image": "
""" + image + """
",
                            "createOptions": "
""" + createOptions + """
"
                        },
                        "type": "docker",
                        "status": "running",
                        "restartPolicy": "always",
                        "version": "1.0"
                    }
                },
                "$edgeHub": {
                }
            }
""");
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
        /// <param name="logger"></param>
        public NetcapImage(Gateway gateway, ILogger logger)
        {
            _gateway = gateway;
            _logger = logger;
            _regName = "netcapacr";
        }

        /// <summary>
        /// Create or update
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task CreateOrUpdateAsync(CancellationToken ct = default)
        {
            // Create container registry or update if it already exists in rg
            var rg = await _gateway.GetResourceGroupAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Create netcap module image in {ResourceGroup}.",
                rg.Data.Name);
            var registryResponse = await rg.GetContainerRegistries()
                .CreateOrUpdateAsync(WaitUntil.Completed, _regName,
                new ContainerRegistryData(rg.Data.Location,
                    new ContainerRegistrySku(ContainerRegistrySkuName.Basic))
                {
                    IsAdminUserEnabled = true,
                    PublicNetworkAccess = ContainerRegistryPublicNetworkAccess.Enabled
                }, ct).ConfigureAwait(false);
            var registryKeys = await registryResponse.Value.GetCredentialsAsync(ct)
                .ConfigureAwait(false);

            LoginServer = registryResponse.Value.Data.LoginServer;
            Username = registryKeys.Value.Username;
            Password = registryKeys.Value.Passwords[0].Value;
            Name = LoginServer + "/netcap:latest";

            _logger.LogInformation("Building Image {Image} ...", Name);
            // Build the image and push to the registry
            var quickBuild = new ContainerRegistryDockerBuildContent("Dockerfile",
                new ContainerRegistryPlatformProperties("linux") { Architecture = "amd64" })
            {
                SourceLocation =
                    "https://github.com/Azure/Industrial-IoT.git#docsandttl:samples/Netcap",
                IsPushEnabled = true
            };
            quickBuild.ImageNames.Add(Name);
            var buildResponse = await registryResponse.Value.GetContainerRegistryTaskRuns()
                .CreateOrUpdateAsync(WaitUntil.Started, "netcap", new ContainerRegistryTaskRunData
                {
                    RunRequest = quickBuild
                }, ct).ConfigureAwait(false);

            var runs = await registryResponse.Value.GetContainerRegistryTaskRuns()
                .GetAsync("netcap", ct).ConfigureAwait(false);
            var run = await registryResponse.Value.GetContainerRegistryRuns().GetAsync(
                 runs.Value.Data.RunResult.RunId, ct).ConfigureAwait(false);
            var url = await run.Value.GetLogSasUrlAsync(ct).ConfigureAwait(false);
            var client = new BlobClient(new Uri(url.Value.LogLink));
            using var os = Console.OpenStandardOutput();
            using var stream = await client.OpenReadAsync(new BlobOpenReadOptions(false)
            {
                BufferSize = 1024 * 1024
            }, ct).ConfigureAwait(false);

            await Task.WhenAll(
                stream.CopyToAsync(os, ct),
                buildResponse.WaitForCompletionAsync(ct).AsTask()).ConfigureAwait(false);

            _logger.LogInformation("Image {Image} built with {Result}", Name,
                buildResponse.Value.Data.RunResult);
        }

        /// <summary>
        /// Create or update
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task DeleteAsync(CancellationToken ct)
        {
            var rg = await _gateway.GetResourceGroupAsync(ct).ConfigureAwait(false);
            var registryCollection = rg.GetContainerRegistries();
            if (!await registryCollection.ExistsAsync(_regName, ct).ConfigureAwait(false))
            {
                return;
            }
            var registryResponse = await rg.GetContainerRegistryAsync(_regName,
                ct).ConfigureAwait(false);
            await registryResponse.Value.DeleteAsync(WaitUntil.Completed,
                ct).ConfigureAwait(false);

            LoginServer = null!;
            Username = null!;
            Password = null!;
            Name = null!;
        }

        private readonly Gateway _gateway;
        private readonly ILogger _logger;
        private readonly string _regName;
    }

    /// <summary>
    /// Netcap storage controller
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
            _stgName = "netcapstg";
        }

        /// <summary>
        /// Create or update
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async ValueTask CreateOrUpdateAsync(CancellationToken ct)
        {
            var rg = await _gateway.GetResourceGroupAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Create Storage for netcap module in {Rg}.",
                rg.Data.Name);
            var storageResponse = await rg.GetStorageAccounts()
                .CreateOrUpdateAsync(WaitUntil.Completed, _stgName,
                new StorageAccountCreateOrUpdateContent(
                    new StorageSku(StorageSkuName.PremiumLrs),
                    StorageKind.StorageV2, rg.Data.Location)
                {
                    AllowSharedKeyAccess = true
                }, ct).ConfigureAwait(false);
            var storageName = storageResponse.Value.Data.Name;
            var keys = await storageResponse.Value.GetKeysAsync(
                cancellationToken: ct).ToListAsync(ct).ConfigureAwait(false);
            if (keys.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No keys found for storage account {storageName}");
            }

            // Create connection string for storage account
            ConnectionString = "DefaultEndpointsProtocol=https;" +
                $"AccountName={storageName};AccountKey={keys[0].Value}";
            _logger.LogInformation("Storage {Name} for netcap module created.",
                storageName);
        }

        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async ValueTask DeleteAsync(CancellationToken ct)
        {
            var rg = await _gateway.GetResourceGroupAsync(ct).ConfigureAwait(false);
            var storageCollection = rg.GetStorageAccounts();
            if (!await storageCollection.ExistsAsync(_stgName,
                cancellationToken: ct).ConfigureAwait(false))
            {
                return;
            }
            var storageResponse = await rg.GetStorageAccountAsync(_stgName,
                cancellationToken: ct).ConfigureAwait(false);
            await storageResponse.Value.DeleteAsync(WaitUntil.Completed,
                ct).ConfigureAwait(false);

            ConnectionString = null!;
        }

        private readonly Gateway _gateway;
        private readonly ILogger _logger;
        private readonly string _stgName;
    }

    private Module? _publisher;
    private SubscriptionResource? _subscription;
    private string? _resourceGroupName;
    private string? _connectionString;
    private readonly ILogger _logger;
    private readonly ArmClient _client;
}
