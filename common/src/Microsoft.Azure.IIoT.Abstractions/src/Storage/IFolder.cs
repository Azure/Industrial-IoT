// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Folder
    /// </summary>
    public interface IFolder {

        /// <summary>
        /// Name of the folder
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Create file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IFile> CreateOrOpenFileAsync(string fileName,
            CancellationToken ct = default);

        /// <summary>
        /// Create locked file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="duration"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IFileLock> CreateOrOpenFileLockAsync(string fileName,
            TimeSpan duration, CancellationToken ct = default);

        /// <summary>
        /// Create folder
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IFolder> CreateOrOpenSubFolderAsync(string folder,
            CancellationToken ct = default);

        /// <summary>
        /// Get all files in folder
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<string>> GetAllFilesAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Get all subfolders in folder
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<string>> GetAllSubFoldersAsync(
            CancellationToken ct = default);
    }
}
