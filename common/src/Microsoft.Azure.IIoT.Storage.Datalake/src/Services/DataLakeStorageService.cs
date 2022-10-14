// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Datalake.Default {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Http;
    using global::Azure.Storage;
    using global::Azure.Storage.Files.DataLake;
    using global::Azure.Storage.Files.DataLake.Models;
    using global::Azure.Core;
    using global::Azure;
    using Serilog;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// Datalake storage service
    /// </summary>
    public class DataLakeStorageService : IFileStorage {

        /// <inheritdoc/>
        public Uri Endpoint { get; }

        /// <summary>
        /// Azure Data lake storage service
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="provider"></param>
        public DataLakeStorageService(IDatalakeConfig config, ILogger logger,
            ITokenProvider provider = null) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Get token source for storage
            Endpoint = new Uri($"https://{config.AccountName}.{config.EndpointSuffix}");
            if (string.IsNullOrEmpty(config.AccountKey)) {
                if (provider?.Supports(Http.Resource.Storage) != true) {
                    throw new InvalidConfigurationException(
                        "Missing shared access key or service principal " +
                        "configuration to access storage account.");
                }
                _client = new DataLakeServiceClient(Endpoint,
                    new FileSystemTokenProvider(provider));
            }
            else {
                _client = new DataLakeServiceClient(Endpoint,
                    new StorageSharedKeyCredential(config.AccountName, config.AccountKey));
            }
        }

        /// <inheritdoc/>
        public async Task<IDrive> CreateOrOpenDriveAsync(string driveName,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(driveName)) {
                throw new ArgumentNullException(nameof(driveName));
            }
            var filesystem = _client.GetFileSystemClient(driveName);
            try {
                await filesystem.CreateIfNotExistsAsync(cancellationToken: ct);
                return new FileSystemDrive(filesystem, _logger);
            }
            catch (RequestFailedException ex) {
                ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                return null;
            }
        }

        /// <summary>
        /// File system based drive
        /// </summary>
        private class FileSystemDrive : IDrive {

            /// <inheritdoc/>
            public string Name => _filesystem.Name;

            /// <summary>
            /// Create drive
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="logger"></param>
            public FileSystemDrive(DataLakeFileSystemClient filesystem, ILogger logger) {
                _filesystem = filesystem;
                _logger = logger.ForContext<FileSystemDrive>();
            }

            /// <inheritdoc/>
            public virtual async Task<DateTimeOffset> GetLastModifiedAsync(
                CancellationToken ct) {
                try {
                    return (await GetPropertiesAsync(ct)).LastModified;
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                    return default;
                }
            }

            /// <inheritdoc/>
            public async Task<IFile> CreateOrOpenFileAsync(string fileName,
                CancellationToken ct) {
                var file = _filesystem.GetFileClient(fileName);
                return await FileSystemFile.CreateOrOpenAsync(
                    _logger, file, null, ct);
            }

            /// <inheritdoc/>
            public async Task<IFileLock> CreateOrOpenFileLockAsync(string fileName,
                TimeSpan duration, CancellationToken ct) {
                var file = _filesystem.GetFileClient(fileName);
                return await LeasedFileSystemFile.CreateOrOpenAsync(
                    _logger, file, duration, ct);
            }

            /// <inheritdoc/>
            public async Task<IFolder> CreateOrOpenSubFolderAsync(string folderName,
                CancellationToken ct) {
                var folder = _filesystem.GetDirectoryClient(folderName);
                await folder.CreateIfNotExistsAsync(cancellationToken: ct);
                return new FileSystemFolder(_filesystem, "/", folder, _logger);
            }

            /// <inheritdoc />
            public async Task<IEnumerable<string>> GetAllFilesAsync(CancellationToken ct) {
                var results = new List<string>();
                try {
                    await foreach (var item in _filesystem.GetPathsAsync(cancellationToken: ct)) {
                        if (item.IsDirectory ?? false) {
                            continue;
                        }
                        results.Add(item.Name);
                    }
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                }

                return results;
            }


            /// <inheritdoc />
            public async Task<IEnumerable<string>> GetAllSubFoldersAsync(CancellationToken ct) {
                var results = new List<string>();
                try {
                    await foreach (var item in _filesystem.GetPathsAsync(cancellationToken: ct)) {
                        if (item.IsDirectory ?? false) {
                            results.Add(item.Name);
                        }
                    }
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                }
                return results;
            }

            /// <summary>
            /// Content length retreiver
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task<FileSystemProperties> GetPropertiesAsync(CancellationToken ct) {
                var properties = await _filesystem.GetPropertiesAsync(cancellationToken: ct);
                return properties.Value;
            }

            private readonly DataLakeFileSystemClient _filesystem;
            private readonly ILogger _logger;
        }

        /// <summary>
        /// File system file
        /// </summary>
        private class FileSystemFile : IFile {

            /// <inheritdoc/>
            public string Name => _file.Name;

            /// <summary>
            /// Create file
            /// </summary>
            /// <param name="file"></param>
            /// <param name="leaseId"></param>
            /// <param name="logger"></param>
            private FileSystemFile(DataLakeFileClient file, string leaseId,
                ILogger logger) {
                _file = file;
                _logger = logger.ForContext<FileSystemFile>();
                _conditions = leaseId == null ? null :
                    new DataLakeRequestConditions { LeaseId = leaseId };
            }

            /// <summary>
            /// Create
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="file"></param>
            /// <param name="leaseId"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            internal static async Task<FileSystemFile> CreateOrOpenAsync(ILogger logger,
                DataLakeFileClient file, string leaseId, CancellationToken ct) {
                try {
                    await file.CreateIfNotExistsAsync(cancellationToken: ct);
                    return new FileSystemFile(file, leaseId, logger);
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                    return null;
                }
            }

            /// <inheritdoc/>
            public async Task<long> GetSizeAsync(CancellationToken ct) {
                try {
                    return (await GetPropertiesAsync(ct)).ContentLength;
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                    return default;
                }
            }

            /// <inheritdoc/>
            public async Task<DateTimeOffset> GetLastModifiedAsync(
                CancellationToken ct) {
                try {
                    return (await GetPropertiesAsync(ct)).LastModified;
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                    return default;
                }
            }

            /// <inheritdoc/>
            public async Task AppendAsync(byte[] stream, int count, CancellationToken ct) {
                try {
                    var properties = await GetPropertiesAsync(ct);
                    using (var buffer = new MemoryStream(stream, 0, count)) {
                        var offset = properties.ContentLength;
                        await _file.AppendAsync(buffer, offset,
                            new DataLakeFileAppendOptions {
                                LeaseId = _conditions?.LeaseId
                            }, cancellationToken: ct);
                        await _file.FlushAsync(offset + count,
                            conditions: _conditions, cancellationToken: ct);
                    }
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                }
            }

            /// <inheritdoc/>
            public async Task UploadAsync(Stream stream, CancellationToken ct) {
                try {
                    await Retry.WithExponentialBackoff(_logger,
                        () => _file.UploadAsync(stream, null, _conditions, cancellationToken: ct),
                        e => e is RequestFailedException re &&
                            re.Status == (int)HttpStatusCode.PreconditionFailed);
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                }
            }

            /// <inheritdoc/>
            public async Task DownloadAsync(Stream stream, CancellationToken ct) {
                try {
                    await _file.ReadToAsync(stream, new DataLakeFileReadToOptions {
                        Conditions = _conditions
                    }, cancellationToken: ct);
                }
                catch (RequestFailedException ex) {
                    if (ex.Status == (int)HttpStatusCode.RequestedRangeNotSatisfiable &&
                        (await GetPropertiesAsync(ct)).ContentLength == 0) {
                        return;
                    }
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                }
            }

            /// <summary>
            /// Content length retreiver
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task<PathProperties> GetPropertiesAsync(CancellationToken ct) {
                var properties = await _file.GetPropertiesAsync(_conditions, ct);
                return properties.Value;
            }

            private readonly DataLakeRequestConditions _conditions;
            private readonly DataLakeFileClient _file;
            private readonly ILogger _logger;
        }

        /// <summary>
        /// Locked file
        /// </summary>
        private class LeasedFileSystemFile : IFileLock {

            /// <inheritdoc/>
            public IFile File { get; }

            /// <summary>
            /// Create file
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="file"></param>
            /// <param name="lease"></param>
            internal LeasedFileSystemFile(ILogger logger,
                FileSystemFile file, DataLakeLeaseClient lease) {
                _logger = logger.ForContext<LeasedFileSystemFile>();
                File = file;
                _lease = lease;
            }

            /// <summary>
            /// Create or open locked file
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="file"></param>
            /// <param name="lockDuration"></param>
            /// <param name="ct"></param>
            internal static async Task<IFileLock> CreateOrOpenAsync(ILogger logger,
                DataLakeFileClient file, TimeSpan lockDuration, CancellationToken ct) {
                // Try acquire lock
                logger = logger.ForContext<LeasedFileSystemFile>();
                var leaseId = Guid.NewGuid().ToString();
                var fileSystemFile = await FileSystemFile.CreateOrOpenAsync(
                    logger, file, leaseId, ct);

                var leased = new LeasedFileSystemFile(logger, fileSystemFile,
                    file.GetDataLakeLeaseClient(leaseId));
                await leased.AcquireAsync(lockDuration, ct);
                return leased;
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync() {
                try {
                    await _lease.ReleaseAsync();
                    _logger.Information("Lease {lease} on {file} released.",
                        _lease.LeaseId, File.Name);
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                }
            }

            /// <summary>
            /// Acquire lock
            /// </summary>
            /// <param name="lockDuration"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task AcquireAsync(TimeSpan lockDuration, CancellationToken ct) {
                if (lockDuration == Timeout.InfiniteTimeSpan) {
                    lockDuration = DataLakeLeaseClient.InfiniteLeaseDuration;
                }
                else if (lockDuration < TimeSpan.FromSeconds(15)) {
                    lockDuration = TimeSpan.FromSeconds(15);
                }
                else if (lockDuration > TimeSpan.FromMinutes(1)) {
                    lockDuration = TimeSpan.FromMinutes(1);
                }
                // Aquire lease
                _logger.Information("Acquiring lease {lease} on {file} for {duration}...",
                    _lease.LeaseId, File.Name, lockDuration);
                try {
                    await Retry.WithLinearBackoff(_logger, ct,
                        () => _lease.AcquireAsync(lockDuration, cancellationToken: ct),
                        e => e is RequestFailedException re &&
                            re.Status == (int)HttpStatusCode.Conflict,
                        int.MaxValue);

                    _logger.Information("Lease {lease} on {file} acquired for {duration}.",
                        _lease.LeaseId, File.Name, lockDuration);
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                }
            }

            private readonly ILogger _logger;
            private readonly DataLakeLeaseClient _lease;
        }

        /// <summary>
        /// File system folder
        /// </summary>
        private class FileSystemFolder : IFolder {

            /// <inheritdoc/>
            public string Name => _folder.Name;

            /// <summary>
            /// Create file
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="parentPath"></param>
            /// <param name="folder"></param>
            /// <param name="logger"></param>
            public FileSystemFolder(DataLakeFileSystemClient filesystem,
                string parentPath, DataLakeDirectoryClient folder, ILogger logger) {
                _filesystem = filesystem;
                _parentPath = parentPath;
                _folder = folder;
                _logger = logger.ForContext<FileSystemFolder>();
            }

            /// <inheritdoc/>
            public virtual async Task<DateTimeOffset> GetLastModifiedAsync(
                CancellationToken ct) {
                try {
                    return (await GetPropertiesAsync(ct)).LastModified;
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                    return default;
                }
            }

            /// <inheritdoc/>
            public async Task<IFile> CreateOrOpenFileAsync(string fileName,
                CancellationToken ct) {
                var file = _folder.GetFileClient(fileName);
                return await FileSystemFile.CreateOrOpenAsync(_logger, file, null, ct);
            }

            /// <inheritdoc/>
            public async Task<IFileLock> CreateOrOpenFileLockAsync(string fileName,
                TimeSpan duration, CancellationToken ct) {
                var file = _folder.GetFileClient(fileName);
                return await LeasedFileSystemFile.CreateOrOpenAsync(_logger, file, duration, ct);
            }

            /// <inheritdoc/>
            public async Task<IFolder> CreateOrOpenSubFolderAsync(
                string folderName, CancellationToken ct) {
                var folder = _folder.GetSubDirectoryClient(folderName);
                try {
                    await folder.CreateIfNotExistsAsync(cancellationToken: ct);
                    return new FileSystemFolder(_filesystem, _parentPath + "/" + Name, folder, _logger);
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                    return null;
                }
            }

            /// <inheritdoc />
            public async Task<IEnumerable<string>> GetAllFilesAsync(CancellationToken ct) {
                var results = new List<string>();
                var path = _parentPath + "/" + Name;
                try {
                    await foreach (var item in _filesystem.GetPathsAsync(path, cancellationToken: ct)) {
                        if (item.IsDirectory ?? false) {
                            continue;
                        }
                        if (item.Name.StartsWith(path, StringComparison.InvariantCultureIgnoreCase)) {
                            results.Add(item.Name.Substring(path.Length + 1));
                        }
                    }
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                    return default;
                }
                return results;
            }

            /// <inheritdoc />
            public async Task<IEnumerable<string>> GetAllSubFoldersAsync(CancellationToken ct) {
                var results = new List<string>();
                var path = _parentPath + "/" + Name;
                try {
                    await foreach (var item in _filesystem.GetPathsAsync(path, cancellationToken: ct)) {
                        if (item.IsDirectory ?? false) {
                            if (item.Name.StartsWith(path, StringComparison.InvariantCultureIgnoreCase)) {
                                results.Add(item.Name.Substring(path.Length + 1));
                            }
                        }
                    }
                }
                catch (RequestFailedException ex) {
                    ((HttpStatusCode)ex.Status).Validate(ex.Message, ex);
                    return default;
                }
                return results;
            }

            /// <summary>
            /// Content length retreiver
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task<PathProperties> GetPropertiesAsync(CancellationToken ct) {
                var properties = await _folder.GetPropertiesAsync(cancellationToken: ct);
                return properties.Value;
            }

            private readonly DataLakeFileSystemClient _filesystem;
            private readonly string _parentPath;
            private readonly DataLakeDirectoryClient _folder;
            private readonly ILogger _logger;
        }

        /// <inheritdoc/>
        private class FileSystemTokenProvider : TokenCredential {

            /// <inheritdoc/>
            public FileSystemTokenProvider(ITokenProvider provider) {
                _provider = provider;
            }

            /// <inheritdoc/>
            public override AccessToken GetToken(
                TokenRequestContext requestContext, CancellationToken ct) {
                return GetTokenAsync(requestContext, ct).AsTask().GetAwaiter().GetResult();
            }

            /// <inheritdoc/>
            public override async ValueTask<AccessToken> GetTokenAsync(
                TokenRequestContext requestContext, CancellationToken ct) {
                var result = await _provider.GetTokenForAsync(Http.Resource.Storage);
                if (result == null) {
                    return default;
                }
                return new AccessToken(result.RawToken, result.ExpiresOn);
            }

            private readonly ITokenProvider _provider;
        }

        private readonly DataLakeServiceClient _client;
        private readonly ILogger _logger;
    }
}
