// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Storage
{
    using Azure.IIoT.OpcUa.Publisher;
    using Furly.Extensions.Storage;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Provider for published nodes file.
    /// </summary>
    public sealed class PublishedNodesProvider : IStorageProvider, IDisposable
    {
        /// <inheritdoc/>
        public event EventHandler<FileSystemEventArgs>? Changed;

        /// <summary>
        /// Provider of storage for published nodes file.
        /// </summary>
        /// <param name="factory">File provider factory</param>
        /// <param name="options">Publisher configuration with location
        /// of published nodes file. </param>
        /// <param name="logger">Logger</param>
        public PublishedNodesProvider(IFileProviderFactory factory,
            IOptions<PublisherOptions> options,
            ILogger<PublishedNodesProvider> logger)
        {
            // TODO: Use IFileProvider and IStorageProvider going forward

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _fileMode = options.Value.PublishedNodesFile == null ||
                options.Value.CreatePublishFileIfNotExist == true ?
                    FileMode.OpenOrCreate : FileMode.Open;
            _fileName = options.Value.PublishedNodesFile ??
                    PublisherConfig.PublishedNodesFileDefault;

            var root = Path.GetDirectoryName(_fileName);
            if (string.IsNullOrWhiteSpace(root))
            {
                root = Environment.CurrentDirectory;
            }
            _root = root;
            _provider = factory.Create(_root);

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
                using var fileStream = new FileStream(_fileName,
                    _fileMode, FileAccess.Read, FileShare.Read);
                return fileStream.ReadAsString(Encoding.UTF8);
            }
            catch (Exception e)
            {
                _logger.ReadContentFailed(e, _fileName);
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
                    using var fileStream = new FileStream(_fileName,
                        _fileMode,
                        FileAccess.Write,
                        // We will require that there is no other process using the file.
                        FileShare.None);
                    fileStream.SetLength(0);
                    fileStream.Write(Encoding.UTF8.GetBytes(content));
                }
                catch (IOException e)
                {
                    _logger.UpdateFileRestrictedShare(_fileName);

                    // We will fall back to writing with ReadWrite access.
                    try
                    {
                        using var fileStream = new FileStream(_fileName,
                            _fileMode,
                            FileAccess.Write,
                            // Relaxing requirements.
                            FileShare.ReadWrite);
                        fileStream.SetLength(0);
                        fileStream.Write(Encoding.UTF8.GetBytes(content));
                        return;
                    }
                    catch (Exception)
                    {
                        _logger.UpdateFileFailed(e, _fileName);
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
                _logger.NoRaisingEvent(currentChangeToken.HasChanged);
                return;
            }
            var exists = File.Exists(_fileName);
            Changed?.Invoke(this, new FileSystemEventArgs(exists ?
                WatcherChangeTypes.Changed : WatcherChangeTypes.Deleted,
                _root, Path.GetFileName(_fileName)));
        }

        private readonly string _root;
        private readonly ILogger _logger;
        private readonly FileMode _fileMode;
        private readonly string _fileName;
        private readonly SemaphoreSlim _lock;
        private readonly IFileProvider _provider;
        private bool _disableRaisingEvents;
        private IChangeToken _watch;
    }

    /// <summary>
    /// Source-generated logging extensions for PublishedNodesProvider
    /// </summary>
    internal static partial class PublishedNodesProviderLogging
    {
        private const int EventClass = 1740;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Debug,
            Message = "Failed to read content of published nodes file from \"{Path}\"")]
        public static partial void ReadContentFailed(this ILogger logger, Exception exception, string path);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Warning,
            Message = "Failed to update published nodes file at \"{Path}\" with restricted share policies. " +
            "Please close any other application that uses this file. Falling back to opening it with more relaxed share policies.")]
        public static partial void UpdateFileRestrictedShare(this ILogger logger, string path);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Error,
            Message = "Failed to update published nodes file at \"{Path}\"")]
        public static partial void UpdateFileFailed(this ILogger logger, Exception exception, string path);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Trace,
            Message = "No raising event while writing ({Changed}).")]
        public static partial void NoRaisingEvent(this ILogger logger, bool changed);
    }
}
