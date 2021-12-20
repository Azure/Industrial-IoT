// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
using Serilog;
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
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _lock;

        /// <summary>
        /// Listens to the file system change notifications and raises events when a directory,
        /// or file in a directory, changes.
        /// </summary>
        public readonly FileSystemWatcher FileSystemWatcher;

        /// <summary>
        /// Provider of utilities for published nodes file.
        /// </summary>
        /// <param name="legacyCliModel"> LegacyCliModel that will define location of published nodes file. </param>
        /// <param name="logger"> Logger </param>
        public PublishedNodesProvider(
            LegacyCliModel legacyCliModel,
            ILogger logger
        ) {
            _legacyCliModel = legacyCliModel ?? throw new ArgumentNullException(nameof(legacyCliModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
            catch (Exception e) {
                _logger.Error(e, "Failed to read content of published nodes file from \"{path}\"",
                    _legacyCliModel.PublishedNodesFile);
                throw;
            }
            finally {
                _lock.Release();
            }

        }

        /// <summary>
        /// Write new content to published nodes file
        /// </summary>
        /// <param name="content"> Content to be written. </param>
        /// <param name="disableRaisingEvents"> If set FileSystemWatcher notifications will be disabled while updating the file.</param>
        public void WriteContent(string content, bool disableRaisingEvents = false) {
            _lock.Wait();
            try {
                // Store current state.
                var eventState = FileSystemWatcher.EnableRaisingEvents;

                if (disableRaisingEvents) {
                    FileSystemWatcher.EnableRaisingEvents = false;
                }

                try {
                    using (var fileStream = new FileStream(
                        _legacyCliModel.PublishedNodesFile,
                        FileMode.Open,
                        FileAccess.Write,
                        // We will require that there is no other process using the file.
                        FileShare.None
                     )) {
                        fileStream.Write(Encoding.UTF8.GetBytes(content));
                    }
                }
                catch (IOException e) {

                    // We will fall back to writing with ReadWrite access.
                    try {
                        using (var fileStream = new FileStream(
                            _legacyCliModel.PublishedNodesFile,
                            FileMode.Open,
                            FileAccess.Write,
                            // Relaxing requirements.
                            FileShare.ReadWrite
                         )) {
                            fileStream.Write(Encoding.UTF8.GetBytes(content));
                        }
                    }
                    catch (Exception) {
                        // Report and raise original exception if fallback also failed.
                        _logger.Error(e, "Failed to update published nodes file at \"{path}\"",
                            _legacyCliModel.PublishedNodesFile);

                        throw e;
                    }
                }

                // Retore state.
                if (disableRaisingEvents) {
                    FileSystemWatcher.EnableRaisingEvents = eventState;
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
