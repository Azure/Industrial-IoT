// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

internal sealed class Uploader
{
    /// <summary>
    /// Create uploader
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="pnJson"></param>
    /// <param name="parameters"></param>
    /// <param name="logger"></param>
    public Uploader(string deviceId, string moduleId, ModuleClient? client,
        string blobConnectionString, ILogger logger)
    {
        _deviceId = deviceId;
        _moduleId = moduleId;
        _client = client;
        _blobConnectionString = blobConnectionString;
        _logger = logger;
    }

    /// <summary>
    /// Upload folder
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask UploadAsync(Bundle bundle, CancellationToken ct)
    {
        BlobContainerClient? containerClient;
        if (!Uri.TryCreate(_blobConnectionString, UriKind.Absolute, out var blobSasUri))
        {
            // Not a sas uri, must be a connection string with key or sas
            var containerName = Extensions.FixContainerName($"{_deviceId}_{_moduleId}");
            containerClient =
                new BlobContainerClient(_blobConnectionString, containerName);
        }
        else
        {
            containerClient = new BlobContainerClient(blobSasUri);
        }

        // Create container
        var containerMetadata = new Dictionary<string, string>
        {
            ["DeviceId"] = _deviceId,
            ["ModuleId"] = _moduleId
        };
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, 
            containerMetadata, cancellationToken: ct).ConfigureAwait(false);
        // Upload capture bundle
        var blobName = $"{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ssZ}";
        var blobClient = containerClient.GetBlobClient(blobName);
        var bundleMetadata = new Dictionary<string, string>
        {
            ["Start"] = bundle.Start.ToString("o"),
            ["End"] = bundle.End.ToString("o"),
            ["Duration"] = (bundle.End - bundle.Start).ToString()
        };
        var bundleFile = bundle.GetBundleFile();
        try
        {
            await blobClient.UploadAsync(bundleFile, new BlobUploadOptions
            {
                Metadata = bundleMetadata,
                ProgressHandler = new Progress<long>(
                    progress => _logger.LogInformation(
                        "Uploading {Progress} bytes", progress))
            }, ct).ConfigureAwait(false);

            if (_client != null)
            {
                // Send completion notification
                await _client.SendEventAsync(new Message(Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(new
                    {
                        ContainerUri = containerClient.Uri,
                        BlobUri = blobClient.Uri,
                        ContainerName = blobClient.BlobContainerName,
                        BlobName = blobClient.Name
                    })))).ConfigureAwait(false);
            }
            bundle.Delete();
        }
        finally
        {
            File.Delete(bundleFile);
        }
    }

    private readonly string _deviceId;
    private readonly string _moduleId;
    private readonly ModuleClient? _client;
    private readonly string _blobConnectionString;
    private ILogger _logger;
}
