// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage {

    /// <summary>
    /// Utilities provider for published nodes file.
    /// </summary>
    public class PublishedNodesProvider: IPublishedNodesProvider, IDisposable {

        private readonly LegacyCliModel _legacyCliModel;
        private readonly SemaphoreSlim _lock;

        /// <summary>
        /// Listens to the file system change notifications and raises events when a directory,
        /// or file in a directory, changes.
        /// </summary>
        public readonly FileSystemWatcher FileSystemWatcher;

        /// <summary>
        /// Provider of utilities for published nodes file.
        /// </summary>
        /// <param name="legacyCliModel"></param>
        public PublishedNodesProvider(LegacyCliModel legacyCliModel) {
            _legacyCliModel = legacyCliModel;

            var directory = Path.GetDirectoryName(_legacyCliModel.PublishedNodesFile);

            if (string.IsNullOrWhiteSpace(directory)) {
                directory = Environment.CurrentDirectory;
            }

            var file = Path.GetFileName(_legacyCliModel.PublishedNodesFile);
            FileSystemWatcher = new FileSystemWatcher(directory, file);

            _lock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Get last write time of published nodes file.
        /// </summary>
        /// <returns></returns>
        public DateTime GetLastWriteTime() {
            _lock.Wait();
            try {
                return File.GetLastWriteTime(_legacyCliModel.PublishedNodesFile);
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Read content of published nodes file.
        /// </summary>
        /// <returns></returns>
        public string ReadContent() {
            _lock.Wait();
            try {
                using (var fileStream = new FileStream(
                    _legacyCliModel.PublishedNodesFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                 )) {
                    return fileStream.ReadAsString(Encoding.UTF8);
                }
            }
            finally {
                _lock.Release();
            }

        }

        /// <summary>
        /// Write new content to published nodes file
        /// </summary>
        /// <param name="content"></param>
        public void WriteContent(string content) {
            _lock.Wait();
            try {
                using (var fileStream = new FileStream(
                    _legacyCliModel.PublishedNodesFile,
                    FileMode.Open,
                    FileAccess.Write,
                    FileShare.None
                 )) {
                    fileStream.Write(Encoding.UTF8.GetBytes(content));
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            FileSystemWatcher?.Dispose();
        }
    }
}
