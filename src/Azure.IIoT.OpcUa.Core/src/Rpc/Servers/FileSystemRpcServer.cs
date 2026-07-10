// ------------------------------------------------------------
//  Copyright (c) Microsoft.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Servers
{
    using Azure.IIoT.OpcUa.Core.Rpc;
            using Azure.IIoT.OpcUa.Core.Storage;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Server that listens for rpc requests on the filesystem. The files that can
    /// act as imput are a simplified version of the .http files used in the REST
    /// Client extension for Visual Studio Code.
    /// </summary>
    public sealed class FileSystemRpcServer : IRpcServer, IDisposable, IAsyncDisposable
    {
        /// <inheritdoc/>
        public string Name => "FileSystem";

        /// <inheritdoc/>
        public IEnumerable<IRpcHandler> Connected
        {
            get
            {
                lock (_handlers)
                {
                    return _handlers.Select(v => v.Server).ToList();
                }
            }
        }

        /// <inheritdoc/>
        public FileSystemRpcServer(IFileProviderFactory fileprovider,
            IOptions<FileSystemRpcServerOptions> options, ILogger<FileSystemRpcServer> logger)
        {
            _logger = logger;
            _processor = Task.CompletedTask;

            var requestPath = options.Value.RequestFilePath ??
                Path.Combine(Environment.CurrentDirectory, "rpc.req");
            _requestFile = Path.GetFileName(requestPath);
            _requestPath = Path.GetDirectoryName(requestPath)
                ?? Environment.CurrentDirectory;
            _requestProvider = fileprovider.Create(_requestPath);

            var responsePath = options.Value.ResponseFilePath ??
                Path.Combine(Environment.CurrentDirectory, "rpc.resp");
            _responseFile = Path.GetFileName(responsePath);
            _responsePath = Path.GetDirectoryName(responsePath)
                ?? Environment.CurrentDirectory;
            _responseProvider = fileprovider.Create(_responsePath);
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (_processor.IsCompleted)
            {
                _tcs.TrySetResult();
                _processor = ProcessAsync(_cts.Token);

                // start change monitoring also
                ChangeCallback(this);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            try
            {
                _disposed = true;
                _tcs.TrySetCanceled();
                await _cts.CancelAsync().ConfigureAwait(false);
                await _processor.ConfigureAwait(false);
            }
            catch
            {
                _logger.ErrorDisposing();
            }
            finally
            {
                _cts.Dispose();
            }
        }

        /// <inheritdoc/>
        public ValueTask<IAsyncDisposable> ConnectAsync(IRpcHandler server,
            CancellationToken ct = default)
        {
            var registration = new Handler(this, server);
            lock (_handlers)
            {
                _handlers.Add(registration);
            }
            return ValueTask.FromResult<IAsyncDisposable>(registration);
        }

        /// <summary>
        /// Process requests
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ProcessAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _tcs.Task.ConfigureAwait(false);
                    _tcs = new TaskCompletionSource();

                    var requestFile = _requestProvider.GetFileInfo(_requestFile);
                    var responseFile = _responseProvider.GetFileInfo(_responseFile);
                    try
                    {
                        if (responseFile.Exists && requestFile.Exists &&
                            responseFile.LastModified == requestFile.LastModified)
                        {
                            // Already processed, dont run again
                            _logger.SkippingProcessedRequest(requestFile.Name);
                            continue;
                        }

                        await DeleteResponseAsync(ct).ConfigureAwait(false);
                        if (!requestFile.Exists)
                        {
                            continue;
                        }

                        //
                        // Cache response before writing so we support the case where
                        // the request restarts the server in error cases and then retry
                        // here.
                        // TODO: Maybe we can do this in a more efficient way.
                        //
                        var response = new MemoryStream();
                        await using (response.ConfigureAwait(false))
                        {
                            var stream = requestFile.CreateReadStream();
                            await using (stream.ConfigureAwait(false))
                            {
                                await DotHttpFileParser.ParseAsync(stream, response, InvokeAsync,
                                    _logger, _responsePath, _responseProvider, ct).ConfigureAwait(false);
                            }
                            // Success: Write response file
                            await WriteResponseAsync(response, requestFile.LastModified,
                                ct).ConfigureAwait(false);
                        }
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    {
                        _logger.ProcessingError(e.Message, requestFile.Name);
                        _logger.RequestError(e, requestFile.Name);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.ExitingProcessor();
                    break;
                }
                catch (Exception ex)
                {
                    _logger.UnexpectedError(ex);
                }
            }

            async Task WriteResponseAsync(Stream response, DateTimeOffset timestamp,
                CancellationToken ct)
            {
                var fi = _responseProvider.GetFileInfo(_responseFile) as IFileInfoEx;
                var p = Path.Combine(_responsePath, _responseFile);
                var stream = fi?.CreateWriteStream() ?? File.Open(p, FileMode.Create);
                await using (stream.ConfigureAwait(false))
                {
                    await response.FlushAsync(ct).ConfigureAwait(false);
                    response.Position = 0;
                    await response.CopyToAsync(stream, ct).ConfigureAwait(false);
                }
                if (fi == null)
                {
                    // Despite the name, last modified property is the last write time
                    File.SetLastWriteTimeUtc(p, timestamp.DateTime);
                    return;
                }
                fi.SetLastModified(timestamp);
            }

            async Task DeleteResponseAsync(CancellationToken ct)
            {
                if (_responseProvider.GetFileInfo(_responseFile) is not IFileInfoEx fi)
                {
                    var p = Path.Combine(_responsePath, _responseFile);
                    if (File.Exists(p))
                    {
                        File.Delete(p);
                    }
                    return;
                }
                if (fi.Exists)
                {
                    await fi.DeleteAsync(ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Register callback
        /// </summary>
        /// <param name="obj"></param>
        private void ChangeCallback(object? obj)
        {
            if (_disposed)
            {
                return;
            }
            var currentChangeToken = _watch;

            _watch = _requestProvider.Watch(_requestFile);
            _watch.RegisterChangeCallback(ChangeCallback, this);

            if (currentChangeToken?.HasChanged == true)
            {
                _tcs.TrySetResult();
            }
        }

        /// <summary>
        /// Handle method invocation
        /// </summary>
        /// <param name="method"></param>
        /// <param name="request"></param>
        /// <param name="headers"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<(int, ReadOnlySequence<byte>)> InvokeAsync(Method method,
            ReadOnlySequence<byte> request, Dictionary<string, string> headers,
            CancellationToken ct)
        {
            if (method.Uri != null)
            {
                // Not yet supported
                return ((int)HttpStatusCode.NotImplemented, default);
            }

            if (!headers.TryGetValue("Content-Type", out var contentType))
            {
                contentType = "application/json";
            }
            foreach (var server in Connected)
            {
                try
                {
                    var result = await server.InvokeAsync(method.String, request,
                        contentType, ct).ConfigureAwait(false);
                    return (200, result);
                }
                catch (MethodCallStatusException mex)
                {
                    return (mex.Details.Status ?? 500,
                        new ReadOnlySequence<byte>(mex.Serialize()));
                }
                catch (NotSupportedException)
                {
                    // Continue
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return ((int)HttpStatusCode.RequestTimeout, default);
                }
                catch (Exception)
                {
                    return ((int)HttpStatusCode.MethodNotAllowed, default);
                }
            }
            return ((int)HttpStatusCode.NotImplemented, default);
        }

        /// <summary>
        /// Registered server
        /// </summary>
        private sealed class Handler : IAsyncDisposable
        {
            /// <summary>
            /// Server
            /// </summary>
            public IRpcHandler Server { get; }

            public Handler(FileSystemRpcServer outer, IRpcHandler server)
            {
                Server = server;
                _outer = outer;
            }

            /// <inheritdoc/>
            public ValueTask DisposeAsync()
            {
                lock (_outer._handlers)
                {
                    _outer._handlers.Remove(this);
                }
                return ValueTask.CompletedTask;
            }
            private readonly FileSystemRpcServer _outer;
        }

        private readonly ILogger<FileSystemRpcServer> _logger;
        private readonly HashSet<Handler> _handlers = [];
        private readonly string _requestFile;
        private readonly string _requestPath;
        private readonly IFileProvider _requestProvider;
        private readonly string _responseFile;
        private readonly string _responsePath;
        private readonly IFileProvider _responseProvider;
        private readonly CancellationTokenSource _cts = new();
        private Task _processor;
        private TaskCompletionSource _tcs = new();
        private IChangeToken? _watch;
        private bool _disposed;
    }

    /// <summary>
    /// Source-generated logging for FileSystemRpcServer
    /// </summary>
    internal static partial class FileSystemRpcServerLogging
    {
        private const int kEventClass = 10;

        [LoggerMessage(EventId = kEventClass + 0, Level = LogLevel.Warning,
            Message = "Error disposing server.")]
        public static partial void ErrorDisposing(this ILogger logger);

        [LoggerMessage(EventId = kEventClass + 1, Level = LogLevel.Information,
            Message = "Skipping {Request} as it was already processed.")]
        public static partial void SkippingProcessedRequest(this ILogger logger, string request);

        [LoggerMessage(EventId = kEventClass + 2, Level = LogLevel.Information,
            Message = "Error {Error} processing request {Request}.")]
        public static partial void ProcessingError(this ILogger logger, string error, string request);

        [LoggerMessage(EventId = kEventClass + 3, Level = LogLevel.Debug,
            Message = "Error processing request {Request}.")]
        public static partial void RequestError(this ILogger logger, Exception e, string request);

        [LoggerMessage(EventId = kEventClass + 4, Level = LogLevel.Debug,
            Message = "Exiting request processor ...")]
        public static partial void ExitingProcessor(this ILogger logger);

        [LoggerMessage(EventId = kEventClass + 5, Level = LogLevel.Error,
            Message = "Unexpected error during request processing ...")]
        public static partial void UnexpectedError(this ILogger logger, Exception ex);
    }
}
