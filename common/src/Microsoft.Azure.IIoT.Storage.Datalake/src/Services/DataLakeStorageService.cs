// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Datalake.Default {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Exceptions;
    using global::Azure.Storage;
    using global::Azure.Storage.Files.DataLake;
    using global::Azure.Storage.Files.DataLake.Models;
    using global::Azure.Core;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

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
        /// <param name="provider"></param>
        public DataLakeStorageService(IDatalakeConfig config, ITokenProvider provider = null) {

            // Get token source for storage
            Endpoint = new Uri($"https://{config.AccountName}.{config.EndpointSuffix}");
            if (provider?.Supports(Http.Resource.Storage) != true) {
                if (string.IsNullOrEmpty(config.AccountKey)) {
                    throw new InvalidConfigurationException(
                        "Missing shared access key or service principal " +
                        "configuration to access storage account.");
                }
                _client = new DataLakeServiceClient(Endpoint,
                    new StorageSharedKeyCredential(config.AccountName, config.AccountKey));
            }
            else {
                _client = new DataLakeServiceClient(Endpoint,
                    new FileSystemTokenProvider(provider));
            }
        }

        /// <inheritdoc/>
        public async Task<IDrive> CreateOrOpenDriveAsync(string driveName,
            CancellationToken ct) {
            if (string.IsNullOrEmpty(driveName)) {
                throw new ArgumentNullException(nameof(driveName));
            }
            var filesystem = _client.GetFileSystemClient(driveName);
            await filesystem.CreateIfNotExistsAsync(cancellationToken: ct);
            return new FileSystemDrive(filesystem);
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
            public FileSystemDrive(DataLakeFileSystemClient filesystem) {
                _filesystem = filesystem;
            }

            /// <inheritdoc/>
            public virtual async Task<DateTimeOffset> GetLastModifiedAsync(
                CancellationToken ct) {
                return (await GetPropertiesAsync(ct)).LastModified;
            }

            /// <inheritdoc/>
            public async Task<IFile> CreateOrOpenFileAsync(string fileName,
                CancellationToken ct) {
                var file = _filesystem.GetFileClient(fileName);
                await file.CreateIfNotExistsAsync(cancellationToken: ct);
                return new FileSystemFile(file);
            }

            /// <inheritdoc/>
            public async Task<IFolder> CreateOrOpenSubFolderAsync(string folderName,
                CancellationToken ct) {
                var folder = _filesystem.GetDirectoryClient(folderName);
                await folder.CreateIfNotExistsAsync(cancellationToken: ct);
                return new FileSystemFolder(_filesystem, "/", folder);
            }

            /// <inheritdoc />
            public async Task<IEnumerable<string>> GetAllFilesAsync(CancellationToken ct) {
                var results = new List<string>();
                await foreach (var item in _filesystem.GetPathsAsync(cancellationToken: ct)) {
                    if (item.IsDirectory ?? false) {
                        continue;
                    }
                    results.Add(item.Name);
                }
                return results;
            }


            /// <inheritdoc />
            public async Task<IEnumerable<string>> GetAllSubFoldersAsync(CancellationToken ct) {
                var results = new List<string>();
                await foreach (var item in _filesystem.GetPathsAsync(cancellationToken: ct)) {
                    if (item.IsDirectory ?? false) {
                        results.Add(item.Name);
                    }
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
        }

        /// <summary>
        /// File system file
        /// </summary>
        private class FileSystemFile : IFile {

            /// <inheritdoc/>
            public string Name => _file.Name;

            public DateTimeOffset LastModified { get; }

            /// <summary>
            /// Create file
            /// </summary>
            /// <param name="file"></param>
            public FileSystemFile(DataLakeFileClient file) {
                _file = file;
            }

            /// <inheritdoc/>
            public virtual async Task<long> GetSizeAsync(CancellationToken ct) {
                return (await GetPropertiesAsync(ct)).ContentLength;
            }

            /// <inheritdoc/>
            public virtual async Task<DateTimeOffset> GetLastModifiedAsync(
                CancellationToken ct) {
                return (await GetPropertiesAsync(ct)).LastModified;
            }

            /// <inheritdoc/>
            public virtual async Task WriteAsync(byte[] stream, int count,
                long offset, CancellationToken ct) {
                using (var buffer = new MemoryStream(stream, 0, count)) {
                    await _file.AppendAsync(buffer, offset, cancellationToken: ct);
                    await _file.FlushAsync(offset + count, cancellationToken: ct);
                }
            }

            /// <inheritdoc/>
            public virtual async Task AppendAsync(byte[] stream, int count, CancellationToken ct) {
                var properties = await GetPropertiesAsync(ct);
                await WriteAsync(stream, count, properties.ContentLength, ct);
            }

            /// <inheritdoc/>
            public virtual async Task UploadAsync(Stream stream, CancellationToken ct) {
                await _file.UploadAsync(stream, true, cancellationToken: ct);
            }

            /// <inheritdoc/>
            public virtual async Task DownloadAsync(Stream stream, CancellationToken ct) {
                await _file.ReadToAsync(stream, cancellationToken: ct);
            }

            /// <summary>
            /// Content length retreiver
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task<PathProperties> GetPropertiesAsync(CancellationToken ct) {
                var properties = await _file.GetPropertiesAsync(cancellationToken: ct);
                return properties.Value;
            }

            private readonly DataLakeFileClient _file;
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
            public FileSystemFolder(DataLakeFileSystemClient filesystem,
                string parentPath, DataLakeDirectoryClient folder) {
                _filesystem = filesystem;
                _parentPath = parentPath;
                _folder = folder;
            }

            /// <inheritdoc/>
            public virtual async Task<DateTimeOffset> GetLastModifiedAsync(
                CancellationToken ct) {
                return (await GetPropertiesAsync(ct)).LastModified;
            }

            /// <inheritdoc/>
            public async Task<IFile> CreateOrOpenFileAsync(string fileName,
                CancellationToken ct) {
                var file = _folder.GetFileClient(fileName);
                await file.CreateIfNotExistsAsync(cancellationToken: ct);
                return new FileSystemFile(file);
            }

            /// <inheritdoc/>
            public async Task<IFolder> CreateOrOpenSubFolderAsync(
                string folderName, CancellationToken ct) {
                var folder = _folder.GetSubDirectoryClient(folderName);
                await folder.CreateIfNotExistsAsync(cancellationToken: ct);
                return new FileSystemFolder(_filesystem, _parentPath + "/" + Name, folder);
            }

            /// <inheritdoc />
            public async Task<IEnumerable<string>> GetAllFilesAsync(CancellationToken ct) {
                var results = new List<string>();
                var path = _parentPath + "/" + Name;
                await foreach (var item in _filesystem.GetPathsAsync(path, cancellationToken: ct)) {
                    if (item.IsDirectory ?? false) {
                        continue;
                    }
                    if (item.Name.StartsWith(path, StringComparison.InvariantCultureIgnoreCase)) {
                        results.Add(item.Name.Substring(path.Length + 1));
                    }
                }
                return results;
            }

            /// <inheritdoc />
            public async Task<IEnumerable<string>> GetAllSubFoldersAsync(CancellationToken ct) {
                var results = new List<string>();
                var path = _parentPath + "/" + Name;
                await foreach (var item in _filesystem.GetPathsAsync(path, cancellationToken: ct)) {
                    if (item.IsDirectory ?? false) {
                        if (item.Name.StartsWith(path, StringComparison.InvariantCultureIgnoreCase)) {
                            results.Add(item.Name.Substring(path.Length + 1));
                        }
                    }
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
                return GetTokenAsync(requestContext, ct).Result;
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
    }
}
