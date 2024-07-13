// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using Azure;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Netcap storage
/// </summary>
internal record class Storage
{
    /// <summary>
    /// Connections string
    /// </summary>
    public string ConnectionString { get; private set; } = null!;

    /// <summary>
    /// Create storage
    /// </summary>
    /// <param name="iothub"></param>
    /// <param name="logger"></param>
    public Storage(IoTHub iothub, ILogger logger)
    {
        _iothub = iothub;
        _logger = logger;
        _name = _iothub.Name + "netcap-stg";
    }

    /// <summary>
    /// Create or update
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async ValueTask CreateOrUpdateAsync(CancellationToken ct)
    {
        var rg = await _iothub.GetResourceGroupAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Create Storage for netcap module in {Rg}.",
            rg.Data.Name);
        var storageResponse = await rg.GetStorageAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, _name,
            new StorageAccountCreateOrUpdateContent(
                new StorageSku(StorageSkuName.PremiumLrs),
                StorageKind.StorageV2, rg.Data.Location)
            {
                AllowSharedKeyAccess = true,
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
        ConnectionString = $"DefaultEndpointsProtocol=https;" +
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
        var rg = await _iothub.GetResourceGroupAsync(ct).ConfigureAwait(false);
        var storageCollection = rg.GetStorageAccounts();
        if (!await storageCollection.ExistsAsync(_name, 
            cancellationToken: ct).ConfigureAwait(false))
        {
            return;
        }
        var storageResponse = await rg.GetStorageAccountAsync(_name,
            cancellationToken: ct).ConfigureAwait(false);
        await storageResponse.Value.DeleteAsync(WaitUntil.Completed,
            ct).ConfigureAwait(false);

        ConnectionString = null!;
    }

    private readonly IoTHub _iothub;
    private readonly ILogger _logger;
    private readonly string _name;
}

