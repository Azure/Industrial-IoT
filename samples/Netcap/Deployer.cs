// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using Azure.Identity;
using Azure.ResourceManager;
using Microsoft.Extensions.Logging;

/// <summary>
/// Deploy the netcap module to an IoT edge device
/// </summary>
internal sealed class Deployer
{
    /// <summary>
    /// Create deployer
    /// </summary>
    /// <param name="logger"></param>
    public Deployer(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Deploy to azure
    /// </summary>
    /// <returns></returns>
    public async Task RunAsync(CancellationToken ct = default)
    {
        // Login to azure
        var armClient = new ArmClient(new DefaultAzureCredential());

        // Get publisher
        var iothub = new IoTHub(armClient, _logger);
        await iothub.SelectPublisherAsync(ct).ConfigureAwait(false);

        // Create storage account or update if it already exists in the rg
        var storage = new Storage(iothub, _logger);
        await storage.CreateOrUpdateAsync(ct).ConfigureAwait(false);

        // Create container registry or update and build netcap module
        var image = new Image(iothub, _logger);
        await image.CreateOrUpdateAsync(ct).ConfigureAwait(false);

        // Deploy the module using manifest to device with the chosen publisher
        await iothub.DeployModuleAsync(image, storage, ct).ConfigureAwait(false);

        // Wait until the deployment is complete and data is arriving 
        // ...
    }

    private readonly ILogger _logger;
}

