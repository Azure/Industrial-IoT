// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Storage
{
    using Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Utilities provider for published nodes file.
    /// </summary>
    public sealed class PublishedNodesProvider : IStorageProvider, IDisposable
    {
        /// <inheritdoc/>
        public event EventHandler<FileSystemEventArgs>? Deleted;

        /// <inheritdoc/>
        public event EventHandler<FileSystemEventArgs>? Created;

        /// <inheritdoc/>
        public event EventHandler<FileSystemEventArgs>? Changed;

        /// <inheritdoc/>
        public event EventHandler<RenamedEventArgs>? Renamed;

        /// <inheritdoc/>
        public bool EnableRaisingEvents
        {
            get { return _fileSystemWatcher.EnableRaisingEvents; }
            set { _fileSystemWatcher.EnableRaisingEvents = value; }
        }

        /// <summary>
        /// Get file mode to use
        /// </summary>
        private FileMode FileMode =>
            _options.Value.PublishedNodesFile == null ||
            _options.Value.CreatePublishFileIfNotExist == true ?
                FileMode.OpenOrCreate : FileMode.Open;

        /// <summary>
        /// Provider of utilities for published nodes file.
        /// </summary>
        /// <param name="options"> Publisher configuration with location
        /// of published nodes file. </param>
        /// <param name="logger"> Logger </param>
        public PublishedNodesProvider(IOptions<PublisherOptions> options,
            ILogger<PublishedNodesProvider> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _fileName = _options.Value.PublishedNodesFile ??
                PublisherConfig.PublishedNodesFileDefault;
            var directory = Path.GetDirectoryName(_fileName);

            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Environment.CurrentDirectory;
            }

            var file = Path.GetFileName(_fileName);
            _fileSystemWatcher = new FileSystemWatcher(directory, file);
            _fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
            _fileSystemWatcher.Created += FileSystemWatcher_Created;
            _fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            _fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
            _fileSystemWatcher.EnableRaisingEvents = true;

            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public DateTime GetLastWriteTime()
        {
            _lock.Wait();
            try
            {
                return File.GetLastWriteTime(_fileName);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public string ReadContent()
        {
            _lock.Wait();
            try
            {
                // Create file only if it is the default file.
                using (var fileStream = new FileStream(_fileName,
                    FileMode, FileAccess.Read, FileShare.Read))
                {
                    return fileStream.ReadAsString(Encoding.UTF8);
                }
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "Failed to read content of published nodes file from \"{Path}\"",
                    _fileName);
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void WriteContent(string content, bool disableRaisingEvents = false)
        {
            _lock.Wait();
            // Store current state.
            var eventState = _fileSystemWatcher.EnableRaisingEvents;
            try
            {
                if (disableRaisingEvents)
                {
                    _fileSystemWatcher.EnableRaisingEvents = false;
                }

                try
                {
                    using (var fileStream = new FileStream(_fileName,
                        FileMode,
                        FileAccess.Write,
                        // We will require that there is no other process using the file.
                        FileShare.None))
                    {
                        fileStream.SetLength(0);
                        fileStream.Write(Encoding.UTF8.GetBytes(content));
                    }
                }
                catch (IOException e)
                {
                    _logger.LogWarning(
                        "Failed to update published nodes file at \"{Path}\" with restricted share policies. " +
                        "Please close any other application that uses this file. " +
                        "Falling back to opening it with more relaxed share policies.",
                        _fileName);

                    // We will fall back to writing with ReadWrite access.
                    try
                    {
                        using (var fileStream = new FileStream(_fileName,
                            FileMode,
                            FileAccess.Write,
                            // Relaxing requirements.
                            FileShare.ReadWrite))
                        {
                            fileStream.SetLength(0);
                            fileStream.Write(Encoding.UTF8.GetBytes(content));
                        }
                        return;
                    }
                    catch (Exception)
                    {
                        // Report and raise original exception if fallback also failed.
                        _logger.LogError(e, "Failed to update published nodes file at \"{Path}\"",
                            _fileName);
                    }
                    throw;
                }
            }
            finally
            {
                // Retore state.
                if (disableRaisingEvents)
                {
                    _fileSystemWatcher.EnableRaisingEvents = eventState;
                }

                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            _fileSystemWatcher.Deleted -= FileSystemWatcher_Deleted;
            _fileSystemWatcher.Created -= FileSystemWatcher_Created;
            _fileSystemWatcher.Changed -= FileSystemWatcher_Changed;
            _fileSystemWatcher.Renamed -= FileSystemWatcher_Renamed;

            _fileSystemWatcher.Dispose();
            _lock.Dispose();
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Changed?.Invoke(sender, e);
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            Created?.Invoke(sender, e);
        }

        private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Deleted?.Invoke(sender, e);
        }

        private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            Renamed?.Invoke(sender, e);
        }

        private readonly IOptions<PublisherOptions> _options;
        private readonly ILogger _logger;
        private readonly string _fileName;
        private readonly SemaphoreSlim _lock;
        private readonly FileSystemWatcher _fileSystemWatcher;
    }
}
