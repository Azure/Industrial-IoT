// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using Azure;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.ContainerRegistry.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Container image
/// </summary>
internal record class Image
{
    public string LoginServer { get; private set; } = null!;
    public string Username { get; private set; } = null!;
    public string Password { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Create image
    /// </summary>
    /// <param name="iothub"></param>
    /// <param name="logger"></param>
    public Image(IoTHub iothub, ILogger logger)
    {
        _iothub = iothub;
        _logger = logger;
        _name = _iothub.Name + "netcap-acr";
    }

    /// <summary>
    /// Create or update
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask CreateOrUpdateAsync(CancellationToken ct = default)
    {
        // Create container registry or update if it already exists in rg
        var rg = await _iothub.GetResourceGroupAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Create Image of netcap module in {Rg}.",
            rg.Data.Name);
        var registryResponse = await rg.GetContainerRegistries()
            .CreateOrUpdateAsync(WaitUntil.Completed, _name,
            new ContainerRegistryData(rg.Data.Location,
                new ContainerRegistrySku(ContainerRegistrySkuName.Basic))
            {
                IsAdminUserEnabled = true,
                PublicNetworkAccess = ContainerRegistryPublicNetworkAccess.Enabled,
            }, ct).ConfigureAwait(false);
        var registryKeys = await registryResponse.Value.GetCredentialsAsync(ct)
            .ConfigureAwait(false);

        LoginServer = registryResponse.Value.Data.LoginServer;
        Username = registryKeys.Value.Username;
        Password = registryKeys.Value.Passwords.First().Value;
        Name = LoginServer + "/netcap:latest";

        _logger.LogInformation("Build Image {Image} ...", Name);
        // Build the image and push to the registry
        var buildStep = new ContainerRegistryDockerBuildStep("Dockerfile")
        {
            ContextPath = 
            "https://github.com/Azure/Industrial-IoT.git#main:Samples/Netcap",
            IsPushEnabled = true,
        };
        buildStep.ImageNames.Add(Name);
        var buildResponse = await registryResponse.Value.GetContainerRegistryTasks()
            .CreateOrUpdateAsync(WaitUntil.Completed, "netcap",
            new ContainerRegistryTaskData(rg.Data.Location)
            {
                Step = buildStep,
                Platform = new ContainerRegistryPlatformProperties("linux/amd64")
            }, ct).ConfigureAwait(false);
        _logger.LogInformation("Image {Image} built.", Name);
    }

    /// <summary>
    /// Create or update
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask DeleteAsync(CancellationToken ct)
    {
        var rg = await _iothub.GetResourceGroupAsync(ct).ConfigureAwait(false);
        var registryCollection = rg.GetContainerRegistries();
        if (!await registryCollection.ExistsAsync(_name, ct).ConfigureAwait(false))
        {
            return;
        }
        var registryResponse = await rg.GetContainerRegistryAsync(_name, 
            ct).ConfigureAwait(false);
        await registryResponse.Value.DeleteAsync(WaitUntil.Completed,
            ct).ConfigureAwait(false);

        LoginServer = null!;
        Username = null!;
        Password = null!;
        Name = null!;
    }

    private readonly IoTHub _iothub;
    private readonly ILogger _logger;
    private readonly string _name;
}

