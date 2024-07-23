// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;
using System.Globalization;
using Microsoft.Azure.Devices.Shared;
using Azure;
using System.IO.Compression;

/// <summary>
/// Upload and download bundles
/// </summary>
internal sealed class Storage
{
    /// <summary>
    /// CreateSidecarDeployment capture sync
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="moduleId"></param>
    /// <param name="connectionString"></param>
    /// <param name="logger"></param>
    public Storage(string deviceId, string moduleId, string connectionString,
        ILogger logger)
    {
        _connectionString = connectionString;
        _deviceId = deviceId;
        _moduleId = moduleId;
        _logger = logger;
    }

    /// <summary>
    /// Download
    /// </summary>
    /// <param name="path"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task DownloadAsync(string path, CancellationToken ct = default)
    {
        var name = Extensions.FixUpStorageName($"{_deviceId}_{_moduleId}");
        var queueClient = new QueueClient(_connectionString, name);
        await EnsureQueueAsync(queueClient, ct).ConfigureAwait(false);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        _logger.LogInformation("Downloading capture bundles to {Path}.", path);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Receive message
                var message = await queueClient.ReceiveMessageAsync(
                    cancellationToken: ct).ConfigureAwait(false);
                if (!message.HasValue || message.Value?.Body == null)
                {
                    continue;
                }
                var notification =
                    JsonSerializer.Deserialize<Notification>(message.Value.Body);
                if (notification == null)
                {
                    continue;
                }
                try
                {
                    var containerClient = new BlobContainerClient(_connectionString,
                        notification.ContainerName);
                    var blobClient = containerClient.GetBlobClient(notification.BlobName);

                    var blobProperties = await blobClient.GetPropertiesAsync(
                        cancellationToken: ct).ConfigureAwait(false);
                    var metadata = blobProperties.Value.Metadata;
                    if (metadata.TryGetValue("Start", out var s) &&
                        metadata.TryGetValue("End", out var e) &&
                        DateTimeOffset.TryParse(s,
                            CultureInfo.InvariantCulture, out var start) &&
                        DateTimeOffset.TryParse(e,
                            CultureInfo.InvariantCulture, out var end))
                    {
                        var file = Path.Combine(path, notification.BlobName);
                        await blobClient.DownloadToAsync(file, cancellationToken: ct)
                            .ConfigureAwait(false);

                        _logger.LogInformation("Downloaded capture bundle {File}.", file);
                        var folder = Path.Combine(path, Path.GetFileNameWithoutExtension(file));
                        ZipFile.ExtractToDirectory(file, folder);
                        _logger.LogInformation("Unpacked file {File} to {Folder}.", file,
                            folder);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to download capture bundle {Bundle}.",
                        notification.BlobName);
                }
                finally
                {
                    if (message.Value?.MessageId != null)
                    {
                        await queueClient.DeleteMessageAsync(message.Value.MessageId,
                            message.Value.PopReceipt, ct).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving download notification.");
            }
        }
    }

    /// <summary>
    /// Upload bundle
    /// </summary>
    /// <param name="bundle"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask UploadAsync(Bundle bundle, CancellationToken ct = default)
    {
        try
        {
            var bundleFile = bundle.GetBundleFile();
            _logger.LogInformation("Uploading capture bundle {File}.", bundleFile);
            var name = Extensions.FixUpStorageName($"{_deviceId}_{_moduleId}");
            var containerClient = new BlobContainerClient(_connectionString, name);
            var queueClient = new QueueClient(_connectionString, name);
            await EnsureQueueAsync(queueClient, ct).ConfigureAwait(false);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None,
                GetClientMetadata(), cancellationToken: ct).ConfigureAwait(false);

            // Upload capture bundle
            var blobName = $"{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ssZ}.zip";
            var blobClient = containerClient.GetBlobClient(blobName);
            var bundleMetadata = new Dictionary<string, string>
            {
                ["Start"] = bundle.Start.ToString("o"),
                ["End"] = bundle.End.ToString("o"),
                ["Duration"] = (bundle.End - bundle.Start).ToString()
            };
            try
            {
                await blobClient.UploadAsync(bundleFile, new BlobUploadOptions
                {
                    Metadata = bundleMetadata,
                    ProgressHandler = new Progress<long>(
                        progress => _logger.LogInformation(
                            "Uploading {Progress} bytes", progress))
                }, ct).ConfigureAwait(false);

                if (queueClient != null)
                {
                    // Send completion notification
                    var message = JsonSerializer.Serialize(new Notification(
                        containerClient.Uri, blobClient.Uri,
                        blobClient.BlobContainerName, blobClient.Name));
                    await queueClient.SendMessageAsync(message, ct).ConfigureAwait(false);
                }
                bundle.Delete();
                _logger.LogInformation("Completed upload of bundle {File}.", bundleFile);
            }
            finally
            {
                File.Delete(bundleFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload capture bundle.");
            throw;
        }
    }

    /// <summary>
    /// Cleanup storage
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task DeleteAsync(CancellationToken ct)
    {
        var name = Extensions.FixUpStorageName($"{_deviceId}_{_moduleId}");
        _logger.LogInformation("Delete storage {Name}...", name);

        var containerClient = new BlobContainerClient(_connectionString, name);
        if (await containerClient.ExistsAsync(ct).ConfigureAwait(false))
        {
            // leave
            // await containerClient.DeleteAsync(cancellationToken: ct).ConfigureAwait(false);
        }
        var queueClient = new QueueClient(_connectionString, name);
        if (await queueClient.ExistsAsync(ct).ConfigureAwait(false))
        {
            await queueClient.DeleteAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Ensure queue exists and can be used
    /// </summary>
    /// <param name="queueClient"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task EnsureQueueAsync(QueueClient queueClient, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await queueClient.CreateIfNotExistsAsync(GetClientMetadata(),
                    ct).ConfigureAwait(false);
                break;
            }
            catch (RequestFailedException ex)
                when (ex.Status == 409 && ex.ErrorCode == "QueueBeingDeleted")
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
            }
        }
    }

    private Dictionary<string, string> GetClientMetadata()
    {
        return new Dictionary<string, string>
        {
            ["DeviceId"] = _deviceId,
            ["ModuleId"] = _moduleId
        };
    }

    internal sealed record class Notification(Uri ContainerUri, Uri BlobUri,
        string ContainerName, string BlobName);

    private readonly string _deviceId;
    private readonly string _moduleId;
    private readonly string _connectionString;
    private readonly ILogger _logger;
}
