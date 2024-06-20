// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Storage
{
    using Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
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
        public event EventHandler<FileSystemEventArgs>? Changed;

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

            _provider = new PhysicalFileProvider(directory);
            if (_options.Value.UseFileChangePolling == true)
            {
                _provider.UseActivePolling = true;
                _provider.UsePollingFileWatcher = true;
            }
            _watch = _provider.Watch(Path.GetFileName(_fileName));
            _watch.RegisterChangeCallback(ChangeCallback, this);

            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public DateTime GetLastWriteTime()
        {
            _lock.Wait();
            try
            {
                var fileName = Path.GetFileName(_fileName);
                return _provider.GetFileInfo(fileName).LastModified.UtcDateTime;
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
                if (!File.Exists(_fileName))
                {
                    return string.Empty;
                }
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
            try
            {
                if (disableRaisingEvents)
                {
                    _disableRaisingEvents = true;
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
                    _disableRaisingEvents = false;
                }

                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _provider.Dispose();
            _lock.Dispose();
        }

        /// <summary>
        /// Register callback
        /// </summary>
        /// <param name="obj"></param>
        private void ChangeCallback(object? obj)
        {
            var currentChangeToken = _watch;
            _watch = _provider.Watch(Path.GetFileName(_fileName));
            _watch.RegisterChangeCallback(ChangeCallback, this);
            if (!currentChangeToken.HasChanged || _disableRaisingEvents)
            {
                _logger.LogTrace("No raising event while writing ({Changed}).",
                    currentChangeToken.HasChanged);
                return;
            }
            var exists = File.Exists(_fileName);
            Changed?.Invoke(this, new FileSystemEventArgs(exists ?
                WatcherChangeTypes.Changed : WatcherChangeTypes.Deleted,
                _provider.Root, Path.GetFileName(_fileName)));
        }

        private readonly IOptions<PublisherOptions> _options;
        private readonly ILogger _logger;
        private readonly string _fileName;
        private readonly SemaphoreSlim _lock;
        private readonly PhysicalFileProvider _provider;
        private bool _disableRaisingEvents;
        private IChangeToken _watch;
    }
}
