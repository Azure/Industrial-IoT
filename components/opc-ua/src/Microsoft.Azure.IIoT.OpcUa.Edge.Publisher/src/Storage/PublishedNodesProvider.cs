// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage {

    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Serilog;
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Utilities provider for published nodes file.
    /// </summary>
    public class PublishedNodesProvider: IPublishedNodesProvider, IDisposable {

        private readonly LegacyCliModel _legacyCliModel;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _lock;
        private readonly FileSystemWatcher _fileSystemWatcher;

        /// <inheritdoc/>
        public event FileSystemEventHandler Deleted;

        /// <inheritdoc/>
        public event FileSystemEventHandler Created;

        /// <inheritdoc/>
        public event FileSystemEventHandler Changed;

        /// <inheritdoc/>
        public event RenamedEventHandler Renamed;

        /// <inheritdoc/>
        public bool EnableRaisingEvents {
            get { return _fileSystemWatcher.EnableRaisingEvents; }
            set { _fileSystemWatcher.EnableRaisingEvents = value; }
        }

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
            _fileSystemWatcher = new FileSystemWatcher(directory, file);
            _fileSystemWatcher.Deleted += Deleted;
            _fileSystemWatcher.Created += Created;
            _fileSystemWatcher.Changed += Changed;
            _fileSystemWatcher.Renamed += Renamed;

            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public DateTime GetLastWriteTime() {
            _lock.Wait();
            try {
                return File.GetLastWriteTime(_legacyCliModel.PublishedNodesFile);
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void WriteContent(string content, bool disableRaisingEvents = false) {

            // Store current state.
            bool eventState = _fileSystemWatcher.EnableRaisingEvents;

            _lock.Wait();
            try {
                if (disableRaisingEvents) {
                    _fileSystemWatcher.EnableRaisingEvents = false;
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

                    _logger.Warning("Failed to update published nodes file at \"{path}\" with restricted share policies. " +
                        "Please close any other application that uses this file. " +
                        "Falling back to opening it with more relaxed share policies.",
                        _legacyCliModel.PublishedNodesFile);

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

            }
            finally {
                // Retore state.
                if (disableRaisingEvents) {
                    _fileSystemWatcher.EnableRaisingEvents = eventState;
                }

                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            _fileSystemWatcher?.Dispose();
        }
    }
}
