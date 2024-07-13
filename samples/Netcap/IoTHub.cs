// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;
using Azure.ResourceManager;
using Azure.ResourceManager.IotHub;
using Azure.ResourceManager.Resources;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.CompilerServices;

/// <summary>
/// Supports interaction with an IoT Hub
/// </summary>
internal record class IoTHub
{
    public string? Name { get; private set; } = null!;

    /// <summary>
    /// Create iot hub
    /// </summary>
    /// <param name="client"></param>
    /// <param name="logger"></param>
    public IoTHub(ArmClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Select publisher
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async ValueTask SelectPublisherAsync(CancellationToken ct = default)
    {
        var subscriptions = _client.GetSubscriptions().ToList();
        // Get publishers in iot hubs
        var deployments = await GetPublisherDeploymentsAsync(subscriptions,
            ct).ToListAsync(ct).ConfigureAwait(false);

        // Select IoT Hub with publisher modules deployed
        if (deployments.Count == 0)
        {
            throw new InvalidOperationException("No publishers found");
        }
        var selected = deployments[0];
        if (deployments.Count > 1)
        {
            for (var index = 0; index < deployments.Count; index++)
            {
                Console.WriteLine($"{index + 1}: {deployments[index]}");
            }
            var i = -1;
            do { Console.WriteLine("Select index: "); }
            while (!int.TryParse(Console.ReadLine(), out i) ||
                i >= deployments.Count || i < 0);
            selected = deployments[i - 1];
        }

        Name = selected.Hub.Name;

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
    /// <param name="image"></param>
    /// <param name="storage"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask DeployModuleAsync(Image image, Storage storage,
        CancellationToken ct = default)
    {
        if (_publisher == null || _connectionString == null)
        {
            throw new InvalidOperationException("Publisher not selected");
        }

        // Deploy the module using manifest to device with the chosen publisher
        var registryManager = RegistryManager.CreateFromConnectionString(
            _connectionString);
        var ncModuleId = _publisher.Id + "-nc";
        var configId = _publisher.DeviceId + ncModuleId;
        await registryManager.RemoveConfigurationAsync(configId,
            ct).ConfigureAwait(false);
        await registryManager.AddConfigurationAsync(new Configuration(configId)
        {
            TargetCondition = $"deviceId='{_publisher.DeviceId}'",
            Content = new ConfigurationContent
            {
                ModulesContent = Create(_publisher.DeviceId,
                    ncModuleId, _publisher.Id, image.LoginServer, image.Username,
                    image.Password, image.Name, storage.ConnectionString)
            }
        }, ct).ConfigureAwait(false);
        _logger.LogInformation("Deploying netcap module to {DeviceId}...",
            _publisher.DeviceId);

        // Wait until connected
        var connected = false;
        for (var i = 1; !connected; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(i), ct).ConfigureAwait(false);
            var modules = await registryManager.GetModulesOnDeviceAsync(
                _publisher.DeviceId, ct).ConfigureAwait(false);
            connected = modules.Any(m => m.Id == ncModuleId &&
                m.ConnectionState == DeviceConnectionState.Connected);
        }
        _logger.LogInformation("Netcap module connected on {DeviceId}...",
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
    internal record class Deployment(SubscriptionResource Subscription,
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
    /// <param name="subscriptions"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async IAsyncEnumerable<Deployment> GetPublisherDeploymentsAsync(
        List<SubscriptionResource> subscriptions,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var sub in subscriptions)
        {
            await foreach (var hub in sub.GetIotHubDescriptionsAsync(cancellationToken: ct))
            {
                var key = await hub.GetKeysForKeyNameAsync("iothubowner",
                    ct).ConfigureAwait(false);
                var cs = IotHubConnectionStringBuilder.Create(
                    hub.Data.Properties.HostName,
                    new ServiceAuthenticationWithSharedAccessPolicyKey(
                        "iothubowner", key.Value.PrimaryKey));
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

        static async IAsyncEnumerable<Deployment> GetPublishersAsync(
            SubscriptionResource sub, string resourceGroupName, IotHubDescriptionData hub,
            string connectionString, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var service = RegistryManager.CreateFromConnectionString(connectionString);
            var publishers = service.CreateQuery(
                "SELECT * FROM devices.modules WHERE " +
                "properties.reported.__type__ = 'OpcPublisher'", 100);
            string? continuationToken = null;
            while (publishers.HasMoreResults)
            {
                var results = await publishers.GetNextAsTwinAsync(new QueryOptions
                {
                    ContinuationToken = continuationToken
                }).ConfigureAwait(false);
                continuationToken = results.ContinuationToken;
                foreach (var result in results)
                {
                    var version = result.GetProperty("__version__");
                    if (version == null ||
                        !version.StartsWith("2.9.", StringComparison.Ordinal) ||
                        !int.TryParse(version.Split('.')[2], out var patch) ||
                        patch < 10)
                    {
                        // Not supported
                        continue;
                    }
                    yield return new Deployment(sub, resourceGroupName, hub,
                        connectionString, new Module(result.DeviceId, result.ModuleId));
                }
            }
        }
    }

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

    private Module? _publisher;
    private SubscriptionResource? _subscription;
    private string? _resourceGroupName;
    private string? _connectionString;
    private readonly ILogger _logger;
    private readonly ArmClient _client;
}

