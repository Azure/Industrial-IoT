// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Netcap;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;
using System.Globalization;
using System;
using Azure.Storage;

/// <summary>
/// Upload and download files
/// </summary>
internal sealed class Storage
{
    /// <summary>
    /// Create capture sync
    /// </summary>
    /// <param containerName="deviceId"></param>
    /// <param containerName="moduleId"></param>
    /// <param containerName="connectionString"></param>
    /// <param containerName="logger"></param>
    /// <param containerName="runName"></param>
    public Storage(string deviceId, string moduleId, string connectionString,
        ILogger logger, string? runName = null)
    {
        _logger = logger;
        _connectionString = connectionString;
        _deviceId = deviceId;
        _moduleId = moduleId;
        _runName = runName ?? DateTime.UtcNow.ToBinary()
            .ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Download files
    /// </summary>
    /// <param containerName="path"></param>
    /// <param containerName="ct"></param>
    /// <returns></returns>
    public async Task DownloadAsync(string path, CancellationToken ct = default)
    {
        var queueName = Extensions.FixUpStorageName($"{_deviceId}_{_moduleId}");
        var queueClient = new QueueClient(_connectionString, queueName);
        await EnsureQueueAsync(queueClient, ct).ConfigureAwait(false);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        _logger.LogInformation("Downloading files to {Path}.", path);
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
                    if (!metadata.TryGetValue("File", out var f) ||
                        !metadata.TryGetValue("Date", out var d))
                    {
                        continue;
                    }

                    var c = Extensions.FixFolderName(notification.ContainerName);
                    var folder = Path.Combine(path, c);
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    var file = Path.Combine(folder, Extensions.FixFileName(f));
                    _logger.LogInformation("Downloading {Blob}...", notification.BlobName);
                    await blobClient.DownloadToAsync(file, new BlobDownloadToOptions
                    {
                        TransferOptions = new StorageTransferOptions
                        {
                            MaximumConcurrency = 4,
                            InitialTransferSize = 8 * 1024 * 1024,
                            MaximumTransferSize = 8 * 1024 * 1024
                        }
                    }, default).ConfigureAwait(false);
                    _logger.LogInformation("Downloaded {Blob} to file {File}.",
                        notification.BlobName, file);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to download file from blob {BlobName}.",
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
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving download notification.");
            }
        }
    }

    /// <summary>
    /// Upload file
    /// </summary>
    /// <param queueName="file"></param>
    /// <param queueName="ct"></param>
    /// <returns></returns>
    public async ValueTask UploadAsync(string file, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Uploading file {File}.", file);
            var containerName = Extensions.FixUpStorageName($"{_deviceId}_{_moduleId}_{_runName}");
            var containerClient = new BlobContainerClient(_connectionString, containerName);
            var queueName = Extensions.FixUpStorageName($"{_deviceId}_{_moduleId}");
            var queueClient = new QueueClient(_connectionString, queueName);
            await EnsureQueueAsync(queueClient, ct).ConfigureAwait(false);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None,
                GetClientMetadata(), cancellationToken: ct).ConfigureAwait(false);

            // Upload file
            var blobName = Extensions.FixUpStorageName(Path.GetFileName(file));
            var blobClient = containerClient.GetBlobClient(blobName);
            var blobMetadata = new Dictionary<string, string>()
            {
                ["Date"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                ["File"] = Path.GetFileName(file)
            };
            await blobClient.UploadAsync(file, new BlobUploadOptions
            {
                TransferOptions = new StorageTransferOptions
                {
                    MaximumConcurrency = 2,
                    InitialTransferSize = 8 * 1024 * 1024,
                    MaximumTransferSize = 8 * 1024 * 1024
                },
                Metadata = blobMetadata,
                ProgressHandler = new ProgressLogger(_logger, blobName)
            }, ct).ConfigureAwait(false);

            // Send completion notification
            var message = JsonSerializer.Serialize(new Notification(
                containerClient.Uri, blobClient.Uri,
                blobClient.BlobContainerName, blobClient.Name));
            await queueClient.SendMessageAsync(message, ct).ConfigureAwait(false);

            _logger.LogInformation("Completed upload of file {File} to {BlobName}.",
                file, blobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {File}.", file);
            throw;
        }
    }

    /// <summary>
    /// Delete storage
    /// </summary>
    /// <param containerName="ct"></param>
    /// <returns></returns>
    public async Task DeleteAsync(CancellationToken ct)
    {
        var containerName = Extensions.FixUpStorageName($"{_deviceId}_{_moduleId}_{_runName}");
        _logger.LogInformation("Delete storage {Name}...", containerName);

        var containerClient = new BlobContainerClient(_connectionString, containerName);
        if (await containerClient.ExistsAsync(ct).ConfigureAwait(false))
        {
            // leave
            // await containerClient.DeleteAsync(cancellationToken: ct).ConfigureAwait(false);
        }

        var queueName = Extensions.FixUpStorageName($"{_deviceId}_{_moduleId}");
        var queueClient = new QueueClient(_connectionString, queueName);
        if (await queueClient.ExistsAsync(ct).ConfigureAwait(false))
        {
            await queueClient.DeleteAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Ensure queue exists and can be used
    /// </summary>
    /// <param containerName="queueClient"></param>
    /// <param containerName="ct"></param>
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

    private sealed record class ProgressLogger(ILogger Logger, string BlobName) :
        IProgress<long>
    {
        /// <inheritdoc/>
        public void Report(long value)
        {
            if (value > _lastProgress)
            {
                _lastProgress = value;
                Logger.LogInformation(
                    "Uploading {Blob} - {Progress} bytes", BlobName, value);
            }
        }
        private long _lastProgress;
    }

    internal sealed record class Notification(Uri ContainerUri, Uri BlobUri,
        string ContainerName, string BlobName);

    private readonly string _deviceId;
    private readonly string _moduleId;
    private readonly string _runName;
    private readonly string _connectionString;
    private readonly ILogger _logger;
}
