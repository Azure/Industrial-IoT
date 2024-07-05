// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

internal sealed class Uploader : IDisposable
{
    /// <summary>
    /// Create uploader
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="pnJson"></param>
    /// <param name="parameters"></param>
    /// <param name="logger"></param>
    public Uploader(string folder, string? pnJson,
        string deviceId, string moduleId, string blobConnectionString, ILogger logger)
    {
        _folder = folder;
        _pnJson = pnJson;
        _deviceId = deviceId;
        _moduleId = moduleId;
        _blobConnectionString = blobConnectionString;
        _logger = logger;
    }

    /// <summary>
    /// Upload folder
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask UploadAsync(CancellationToken ct)
    {
        BlobContainerClient? containerClient;
        if (!Uri.TryCreate(_blobConnectionString, UriKind.Absolute, out var blobSasUri))
        {
            // Not a sas uri, must be a connection string with key or sas
            var containerName = $"{_deviceId}_{_moduleId}";
            containerClient =
                new BlobContainerClient(_blobConnectionString, containerName);
        }
        else
        {
            containerClient = new BlobContainerClient(blobSasUri);
        }
        var metadata = new Dictionary<string, string>
        {
            ["DeviceId"] = _deviceId,
            ["ModuleId"] = _moduleId
        };
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, metadata,
            cancellationToken: ct).ConfigureAwait(false);
        // Upload capture bundle
        var blobClient = containerClient.GetBlobClient(
            $"{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ssZ}");

        if (_pnJson != null)
        {
            // Add pn.json
            await File.WriteAllTextAsync(Path.Combine(_folder, "pn.json"), _pnJson,
                ct).ConfigureAwait(false);
        }
        var zipFile = Path.Combine(Path.GetTempPath(), "capture-bundle.zip");
        ZipFile.CreateFromDirectory(_folder, zipFile);
        try
        {
            await blobClient.UploadAsync(zipFile, new BlobUploadOptions
            {
                ProgressHandler = new Progress<long>(
                    progress => _logger.LogInformation("Uploading {Progress} bytes", progress))
            }, ct).ConfigureAwait(false);
        }
        finally
        {
            File.Delete(zipFile);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    private string _folder;
    private string? _pnJson;
    private readonly string _deviceId;
    private readonly string _moduleId;
    private readonly string _blobConnectionString;
    private ILogger _logger;
}
