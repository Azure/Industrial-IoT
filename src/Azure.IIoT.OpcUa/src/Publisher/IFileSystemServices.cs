// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Avro.File;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// File system services expose services as per the file transfer specification
    /// https://reference.opcfoundation.org/Core/Part20/v105/docs/#4.3.3.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFileSystemServices<T>
    {
        /// <summary>
        /// Get all file systems on the server
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        IAsyncEnumerable<ServiceResponse<FileSystemObjectModel>> GetFileSystemsAsync(
            T endpoint, CancellationToken ct = default);

        /// <summary>
        /// Get all directories under a filesystem or directory
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="fileSystemOrDirectory"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServiceResponse<IEnumerable<FileSystemObjectModel>>> GetDirectoriesAsync(
            T endpoint, FileSystemObjectModel fileSystemOrDirectory,
            CancellationToken ct = default);

        /// <summary>
        /// Get all files in a directory or filesystem
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="fileSystemOrDirectory"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServiceResponse<IEnumerable<FileSystemObjectModel>>> GetFilesAsync(
            T endpoint, FileSystemObjectModel fileSystemOrDirectory,
            CancellationToken ct = default);

        /// <summary>
        /// Get parent directory or filesystem
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="fileOrDirectoryObject"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServiceResponse<FileSystemObjectModel>> GetParentAsync(T endpoint,
            FileSystemObjectModel fileOrDirectoryObject, CancellationToken ct = default);

        /// <summary>
        /// Get file information for a file
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="file"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServiceResponse<FileInfoModel>> GetFileInfoAsync(T endpoint,
            FileSystemObjectModel file, CancellationToken ct = default);

        /// <summary>
        /// Opens the file for reading. Closing the stream will close the file.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="file"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServiceResponse<Stream>> OpenReadAsync(T endpoint, FileSystemObjectModel file,
            CancellationToken ct = default);

        /// <summary>
        /// Opens the file for writing, closing the stream will close the file.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="file"></param>
        /// <param name="mode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServiceResponse<Stream>> OpenWriteAsync(T endpoint, FileSystemObjectModel file,
            FileWriteMode mode = FileWriteMode.Create, CancellationToken ct = default);

        /// <summary>
        /// Create parent directory under a file system or directory.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="fileSystemOrDirectory"></param>
        /// <param name="name"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServiceResponse<FileSystemObjectModel>> CreateDirectoryAsync(T endpoint,
            FileSystemObjectModel fileSystemOrDirectory, string name,
            CancellationToken ct = default);

        /// <summary>
        /// Create a file in the directory
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="fileSystemOrDirectory"></param>
        /// <param name="name"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServiceResponse<FileSystemObjectModel>> CreateFileAsync(T endpoint,
            FileSystemObjectModel fileSystemOrDirectory, string name,
            CancellationToken ct = default);

        /// <summary>
        /// Delete a file or directory with the name from the directory
        /// or filesystem. If the name is omitted the object is itself
        /// deleted from its parent object.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="fileOrDirectoryObject"></param>
        /// <param name="parentFileSystemOrDirectory"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServiceResultModel> DeleteFileSystemObjectAsync(T endpoint,
            FileSystemObjectModel fileOrDirectoryObject,
            FileSystemObjectModel? parentFileSystemOrDirectory = null,
            CancellationToken ct = default);
    }
}
