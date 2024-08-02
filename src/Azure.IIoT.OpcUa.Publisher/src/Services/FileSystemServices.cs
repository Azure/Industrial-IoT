// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Furly.Exceptions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Buffers;
    using Azure.IIoT.OpcUa.Publisher;
    using DotNetty.Common.Utilities;
    using Azure.Core;

    /// <summary>
    /// This class provides access to a servers address space providing
    /// Filesystem services. It uses the OPC ua client interface to access
    /// the server.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class FileSystemServices<T> : IFileSystemServices<T>, IDisposable
    {
        /// <summary>
        /// Create node service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nodes"></param>
        /// <param name="timeProvider"></param>
        public FileSystemServices(IOpcUaClientManager<T> client,
            INodeServicesInternal<T> nodes, TimeProvider? timeProvider = null)
        {
            _client = client;
            _nodes = nodes;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _activitySource.Dispose();
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<ServiceResponse<FileSystemObjectModel>> GetFileSystemsAsync(
            T endpoint, [EnumeratorCancellation] CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("GetFileSystems");
            var header = new RequestHeaderModel();

            await Task.Delay(0, ct).ConfigureAwait(false);
            yield break;
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ServiceResponse<FileSystemObjectModel>>> GetDirectoriesAsync(
            T endpoint, FileSystemObjectModel fileSystemOrDirectory, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("GetDirectories");
            var header = new RequestHeaderModel();

            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var (nodeId, argInfo) = await GetFileSystemNodeIdAsync(context.Session,
                       header, fileSystemOrDirectory, context.Ct).ConfigureAwait(false);
                if (argInfo != null)
                {
                    new ServiceResponse<FileSystemObjectModel> { ErrorInfo = argInfo }
                         .YieldReturn();
                }
                var (references, errorInfo) = await context.Session.FindAsync(
                    header.ToRequestHeader(_timeProvider), nodeId.YieldReturn(),
                    ReferenceTypeIds.HasComponent, ct: context.Ct).ConfigureAwait(false);
                if (errorInfo != null)
                {
                    new ServiceResponse<FileSystemObjectModel> { ErrorInfo = errorInfo }
                        .YieldReturn();
                }
                return references
                    .Where(r => r.TypeDefinition == Opc.Ua.ObjectTypes.FileDirectoryType)
                    .Select(f => new ServiceResponse<FileSystemObjectModel>
                    {
                        ErrorInfo = f.ErrorInfo,
                        Result = new FileSystemObjectModel
                        {
                            NodeId = AsString(f.Node, context.Session.MessageContext, header),
                            Name = AsString(f.Name, context.Session.MessageContext, header)
                        }
                    });
            }, header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ServiceResponse<FileSystemObjectModel>>> GetFilesAsync(
            T endpoint, FileSystemObjectModel fileSystemOrDirectory, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("GetFiles");
            var header = new RequestHeaderModel();

            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var (nodeId, argInfo) = await GetFileSystemNodeIdAsync(context.Session,
                       header, fileSystemOrDirectory, context.Ct).ConfigureAwait(false);
                if (argInfo != null)
                {
                    new ServiceResponse<FileSystemObjectModel> { ErrorInfo = argInfo }
                         .YieldReturn();
                }

                var (references, errorInfo) = await context.Session.FindAsync(
                    header.ToRequestHeader(_timeProvider), nodeId.YieldReturn(),
                    ReferenceTypeIds.HasComponent, ct: context.Ct).ConfigureAwait(false);
                if (errorInfo != null)
                {
                    new ServiceResponse<FileSystemObjectModel> { ErrorInfo = errorInfo }
                        .YieldReturn();
                }

                return references
                    .Where(r => r.TypeDefinition == Opc.Ua.ObjectTypes.FileType)
                    .Select(f => new ServiceResponse<FileSystemObjectModel>
                    {
                        ErrorInfo = f.ErrorInfo,
                        Result = new FileSystemObjectModel
                        {
                            NodeId = AsString(f.Node, context.Session.MessageContext, header),
                            Name = AsString(f.Name, context.Session.MessageContext, header)
                        }
                    });
            }, header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<Stream>> OpenReadAsync(T endpoint,
            FileSystemObjectModel file, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("OpenRead");
            var header = new RequestHeaderModel();
            var (stream, errorInfo) = await FileTransferStream.OpenAsync(this,
                endpoint, header, file, null, ct).ConfigureAwait(false);
            return new ServiceResponse<Stream>
            {
                ErrorInfo = errorInfo,
                Result = stream
            };
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<Stream>> OpenWriteAsync(T endpoint,
            FileSystemObjectModel file, FileWriteMode mode, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("OpenWrite");
            var header = new RequestHeaderModel();
            var (stream, errorInfo) = await FileTransferStream.OpenAsync(this,
                endpoint, header, file, mode, ct).ConfigureAwait(false);
            return new ServiceResponse<Stream>
            {
                ErrorInfo = errorInfo,
                Result = stream
            };
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<FileSystemObjectModel>> CreateDirectoryAsync(T endpoint,
            FileSystemObjectModel fileSystemOrDirectory, string name, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("CreateDirectory");
            var header = new RequestHeaderModel();
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var (nodeId, argInfo) = await GetFileSystemNodeIdAsync(context.Session,
                     header, fileSystemOrDirectory, context.Ct).ConfigureAwait(false);
                if (argInfo != null)
                {
                    return new ServiceResponse<FileSystemObjectModel> { ErrorInfo = argInfo };
                }
                var requests = new CallMethodRequestCollection
                {
                    new CallMethodRequest
                    {
                        ObjectId = nodeId,
                        MethodId = Opc.Ua.MethodIds.FileDirectoryType_CreateDirectory,
                        InputArguments = new [] { new Variant(name), new Variant(false) }
                    }
                };
                // Call method
                var response = await context.Session.Services.CallAsync(header
                    .ToRequestHeader(_timeProvider), requests, context.Ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, requests);
                if (results.ErrorInfo != null)
                {
                    return new ServiceResponse<FileSystemObjectModel> { ErrorInfo = results.ErrorInfo };
                }
                if (results[0].Result?.OutputArguments == null ||
                    results[0].Result.OutputArguments.Count == 0 ||
                    results[0].Result.OutputArguments[0].Value is not NodeId result)
                {
                    return new ServiceResponse<FileSystemObjectModel>
                    {
                        ErrorInfo = new ServiceResultModel { ErrorMessage = "no node id returned" }
                    };
                }
                return new ServiceResponse<FileSystemObjectModel>
                {
                    Result = new FileSystemObjectModel
                    {
                        NodeId = AsString(result, context.Session.MessageContext, header),
                        Name = name
                    }
                };
            }, header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<FileSystemObjectModel>> CreateFileAsync(T endpoint,
            FileSystemObjectModel fileSystemOrDirectory, string name, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("CreateFile");
            var header = new RequestHeaderModel();
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var (nodeId, argInfo) = await GetFileSystemNodeIdAsync(context.Session,
                     header, fileSystemOrDirectory, context.Ct).ConfigureAwait(false);
                if (argInfo != null)
                {
                    return new ServiceResponse<FileSystemObjectModel> { ErrorInfo = argInfo };
                }

                var requests = new CallMethodRequestCollection
                {
                    new CallMethodRequest
                    {
                        ObjectId = nodeId,
                        MethodId = Opc.Ua.MethodIds.FileDirectoryType_CreateFile,
                        InputArguments = new [] { new Variant(name), new Variant(false) }
                    }
                };
                // Call method
                var response = await context.Session.Services.CallAsync(header
                    .ToRequestHeader(_timeProvider), requests, context.Ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, requests);
                if (results.ErrorInfo != null)
                {
                    return new ServiceResponse<FileSystemObjectModel> { ErrorInfo = results.ErrorInfo };
                }
                if (results[0].Result?.OutputArguments == null ||
                    results[0].Result.OutputArguments.Count == 0 ||
                    results[0].Result.OutputArguments[0].Value is not NodeId result)
                {
                    return new ServiceResponse<FileSystemObjectModel>
                    {
                        ErrorInfo = new ServiceResultModel { ErrorMessage = "no node id returned" }
                    };
                }
                return new ServiceResponse<FileSystemObjectModel>
                {
                    Result = new FileSystemObjectModel
                    {
                        NodeId = AsString(result, context.Session.MessageContext, header),
                        Name = name
                    }
                };
            }, header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResultModel> DeleteFileSystemObjectAsync(T endpoint,
            FileSystemObjectModel parentOrObjectToDelete, string? name, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("DeleteFile");
            var header = new RequestHeaderModel();
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var (nodeId, argInfo) = await GetFileSystemNodeIdAsync(context.Session,
                     header, parentOrObjectToDelete, context.Ct).ConfigureAwait(false);
                if (argInfo != null)
                {
                    return argInfo;
                }

                var targetId = nodeId;
                if (name != null)
                {
                    targetId = await ResolveBrowsePathToNodeAsync(context.Session, header,
                        nodeId, new[] { name }, nameof(name), _timeProvider,
                        context.Ct).ConfigureAwait(false);
                }
                else
                {
                    // Get parent of the targetId by reverting the browse path
                    nodeId = await ResolveBrowsePathToNodeAsync(context.Session, header,
                        targetId, new[] { $"!<HasComponent>{name}" }, nameof(name),
                        _timeProvider, context.Ct).ConfigureAwait(false);
                }
                var requests = new CallMethodRequestCollection
                {
                    new CallMethodRequest
                    {
                        ObjectId = nodeId,
                        MethodId = Opc.Ua.MethodIds.FileDirectoryType_DeleteFileSystemObject,
                        InputArguments = new [] { new Variant(targetId) }
                    }
                };
                // Call method
                var response = await context.Session.Services.CallAsync(header
                    .ToRequestHeader(_timeProvider), requests, context.Ct).ConfigureAwait(false);

                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, requests);
                return results.ErrorInfo ?? new ServiceResultModel();
            }, header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<FileInfoModel>> GetFileInfoAsync(T endpoint,
            FileSystemObjectModel file, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("DeleteFile");
            var header = new RequestHeaderModel();
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var (nodeId, argInfo) = await GetFileSystemNodeIdAsync(context.Session,
                     header, file, context.Ct).ConfigureAwait(false);
                if (argInfo != null)
                {
                    return new ServiceResponse<FileInfoModel> { ErrorInfo = argInfo };
                }
                var (fileInfo, errorInfo) = await context.Session.GetFileInfoAsync(
                    header.ToRequestHeader(_timeProvider), nodeId, context.Ct).ConfigureAwait(false);
                return new ServiceResponse<FileInfoModel>
                {
                    ErrorInfo = errorInfo,
                    Result = fileInfo
                };
            }, header, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Resolve provided path to node.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="rootId"></param>
        /// <param name="paths"></param>
        /// <param name="paramName"></param>
        /// <param name="timeProvider"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        /// <exception cref="ResourceConflictException"></exception>
        private static async Task<NodeId> ResolveBrowsePathToNodeAsync(
            IOpcUaSession session, RequestHeaderModel? header, NodeId rootId,
            string[] paths, string paramName, TimeProvider timeProvider, CancellationToken ct)
        {
            if (paths == null || paths.Length == 0)
            {
                return rootId;
            }
            if (NodeId.IsNull(rootId))
            {
                rootId = ObjectIds.RootFolder;
            }
            var browsepaths = new BrowsePathCollection
            {
                new BrowsePath
                {
                    StartingNode = rootId,
                    RelativePath = paths.ToRelativePath(session.MessageContext)
                }
            };
            var response = await session.Services.TranslateBrowsePathsToNodeIdsAsync(
                header.ToRequestHeader(timeProvider), browsepaths,
                ct).ConfigureAwait(false);
            Debug.Assert(response != null);
            var results = response.Validate(response.Results, r => r.StatusCode,
                response.DiagnosticInfos, browsepaths);
            var count = results[0].Result.Targets?.Count ?? 0;
            if (count == 0)
            {
                throw new ResourceNotFoundException(
                    $"{paramName} did not resolve to any node.");
            }
            if (count != 1)
            {
                throw new ResourceConflictException(
                    $"{paramName} resolved to {count} nodes.");
            }
            return results[0].Result.Targets[0].TargetId
                .ToNodeId(session.MessageContext.NamespaceUris);
        }

        /// <summary>
        /// Get the node id for a file system object
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="fileSystemObject"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private async Task<(NodeId, ServiceResultModel?)> GetFileSystemNodeIdAsync(IOpcUaSession session,
            RequestHeaderModel header, FileSystemObjectModel fileSystemObject,
            CancellationToken ct)
        {
            var nodeId = fileSystemObject.NodeId.ToNodeId(session.MessageContext);
            if (fileSystemObject.BrowsePath?.Count > 0)
            {
                if (nodeId is null)
                {
                    nodeId = ObjectIds.RootFolder;
                }
                nodeId = await ResolveBrowsePathToNodeAsync(session, header,
                    nodeId, fileSystemObject.BrowsePath.ToArray(),
                    nameof(fileSystemObject.BrowsePath), _timeProvider, ct).ConfigureAwait(false);
            }
            if (NodeId.IsNull(nodeId))
            {
                return (NodeId.Null, new ServiceResultModel
                {
                    StatusCode = StatusCodes.BadNodeIdInvalid,
                    ErrorMessage = "Invalid node id and browse path in file system object"
                });
            }
            return (nodeId, null);
        }

        /// <summary>
        /// Convert node id to string
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        internal string AsString(NodeId nodeId,
            IServiceMessageContext context, RequestHeaderModel? header)
        {
            return nodeId.AsString(context, _nodes.GetNamespaceFormat(header)) ?? string.Empty;
        }

        /// <summary>
        /// Convert node id to string
        /// </summary>
        /// <param name="qn"></param>
        /// <param name="context"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        internal string AsString(QualifiedName qn,
            IServiceMessageContext context, RequestHeaderModel? header)
        {
            return qn.AsString(context, _nodes.GetNamespaceFormat(header)) ?? string.Empty;
        }

        /// <summary>
        /// File transfer stream
        /// </summary>
        private class FileTransferStream : Stream
        {
            /// <inheritdoc/>
            public override bool CanRead
                => !_mode.HasValue && _fileHandle.HasValue;

            /// <inheritdoc/>
            public override bool CanWrite
                => _mode.HasValue && _fileHandle.HasValue;

            /// <inheritdoc/>
            public override long Length
                => _fileInfo?.Size ?? Position;

            /// <inheritdoc/>
            public override long Position { get; set; }

            /// <inheritdoc/>
            public override bool CanSeek { get; }

            /// <inheritdoc/>
            public override bool CanTimeout => true;

            /// <inheritdoc/>
            public override int ReadTimeout
            {
                get => _header.OperationTimeout ?? 0;
                set => _header.OperationTimeout = value;
            }

            /// <inheritdoc/>
            public override int WriteTimeout
            {
                get => _header.OperationTimeout ?? 0;
                set => _header.OperationTimeout = value;
            }

            /// <summary>
            /// Create stream
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="handle"></param>
            /// <param name="header"></param>
            /// <param name="nodeId"></param>
            /// <param name="fileHandle"></param>
            /// <param name="fileInfo"></param>
            /// <param name="bufferSize"></param>
            /// <param name="mode"></param>
            public FileTransferStream(FileSystemServices<T> outer,
                ISessionHandle handle, RequestHeaderModel header,
                NodeId nodeId, uint fileHandle, FileInfoModel? fileInfo,
                uint bufferSize, FileWriteMode? mode = null)
            {
                _handle = handle;
                _nodeId = nodeId;
                _fileHandle = fileHandle;
                _outer = outer;
                _header = header;
                _fileInfo = fileInfo;
                _bufferSize = bufferSize;
                _mode = mode;

                if (mode == FileWriteMode.Append)
                {
                    Position = Length;
                }
                else
                {
                    Position = 0;
                }

                CanSeek = false;
            }

            /// <summary>
            /// Open stream
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="endpoint"></param>
            /// <param name="header"></param>
            /// <param name="file"></param>
            /// <param name="mode"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public static async Task<(Stream?, ServiceResultModel?)> OpenAsync(
                FileSystemServices<T> outer, T endpoint, RequestHeaderModel header,
                FileSystemObjectModel file, FileWriteMode? mode = null,
                CancellationToken ct = default)
            {
                var handle = await outer._client.AcquireSessionAsync(endpoint, header,
                    ct).ConfigureAwait(false);
                var closeHandle = handle;
                try
                {
                    var (nodeId, argInfo) = await outer.GetFileSystemNodeIdAsync(handle.Session,
                       header, file, ct).ConfigureAwait(false);
                    if (argInfo != null)
                    {
                        return (null, argInfo);
                    }
                    var (fileInfo, errorInfo) = await handle.Session.GetFileInfoAsync(
                        header.ToRequestHeader(outer._timeProvider),
                        nodeId, ct).ConfigureAwait(false);
                    if (errorInfo != null)
                    {
                        return (null, errorInfo);
                    }
                    var bufferSize = fileInfo?.MaxBufferSize;

                    if (fileInfo?.Writable == false)
                    {
                        return (null, new ServiceResultModel
                        {
                            StatusCode = StatusCodes.BadNotWritable,
                            ErrorMessage = "File is not writable."
                        });
                    }

                    if (bufferSize == null)
                    {
                        var caps = await handle.Session.GetServerCapabilitiesAsync(
                            NamespaceFormat.Index, ct).ConfigureAwait(false);
                        bufferSize = caps.OperationLimits.MaxByteStringLength;
                    }

                    var (fileHandle, errorInfo2) = await handle.Session.OpenAsync(
                        header.ToRequestHeader(outer._timeProvider), nodeId, mode switch
                        {
                            FileWriteMode.Create => 0x6, // Write bit plus erase
                            FileWriteMode.Append => 0x10, // Write bit plus append
                            FileWriteMode.Write => 0x2, // Write bit
                            _ => 0x1 // Read bit
                        }, ct).ConfigureAwait(false);

                    if (errorInfo2 != null)
                    {
                        return (null, errorInfo2);
                    }
                    Debug.Assert(fileHandle.HasValue);
                    closeHandle = null;
                    return (new FileTransferStream(outer, handle, header, nodeId,
                        fileHandle.Value, fileInfo, bufferSize ?? 4096, mode), null);
                }
                finally
                {
                    closeHandle?.Dispose();
                }
            }

            /// <inheritdoc/>
            public override void Flush()
            {
                // No op
            }

            /// <inheritdoc/>
            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                // No op
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException(); // TODO
            }

            /// <inheritdoc/>
            public override void SetLength(long value)
            {
                throw new NotSupportedException(); // TODO
            }

            /// <inheritdoc/>
            public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
                CancellationToken cancellationToken)
            {
                ObjectDisposedException.ThrowIf(!_fileHandle.HasValue, this);
                if (!CanRead)
                {
                    throw new IOException("Cannot read from write-only stream");
                }

                var total = 0;
                while (!_isEoS)
                {
                    var readCount = (int)Math.Min(buffer.Length, _bufferSize);
                    if (readCount == 0)
                    {
                        break;
                    }
                    var (result, errorInfo) = await _handle.Session.ReadAsync(
                        _header.ToRequestHeader(_outer._timeProvider), _nodeId,
                        _fileHandle.Value, readCount, cancellationToken).ConfigureAwait(false);
                    if (errorInfo != null)
                    {
                        throw new IOException(errorInfo.ErrorMessage);
                    }
                    Debug.Assert(result != null);

                    if (result.Length == 0)
                    {
                        // eof
                        _isEoS = true;
                        break;
                    }

                    result.CopyTo(buffer.Span);

                    Position += result.Length;

                    total += result.Length;
                    if (buffer.Length == readCount)
                    {
                        break;
                    }
                    buffer = buffer.Slice(readCount);
                }
                return total;
            }

            /// <inheritdoc/>
            public override int Read(byte[] buffer, int offset, int count)
            {
                var memory = new Memory<byte>(buffer);
                return ReadAsync(memory.Slice(offset, count), default)
                    .AsTask().GetAwaiter().GetResult();
            }

            /// <inheritdoc/>
            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
                CancellationToken cancellationToken)
            {
                ObjectDisposedException.ThrowIf(!_fileHandle.HasValue, this);
                if (!CanWrite)
                {
                    throw new IOException("Cannot write to read-only stream");
                }
                while (true)
                {
                    var writeCount = (int)Math.Min(buffer.Length, _bufferSize);
                    if (writeCount == 0)
                    {
                        break;
                    }
                    var errorInfo = await _handle.Session.WriteAsync(
                        _header.ToRequestHeader(_outer._timeProvider), _nodeId,
                        _fileHandle.Value, buffer.Slice(0, writeCount).ToArray(),
                        cancellationToken).ConfigureAwait(false);
                    if (errorInfo != null)
                    {
                        throw new IOException(errorInfo.ErrorMessage);
                    }

                    Position += writeCount;

                    if (buffer.Length == writeCount)
                    {
                        break;
                    }
                    buffer = buffer.Slice(writeCount);
                }
            }

            /// <inheritdoc/>
            public override void Write(byte[] buffer, int offset, int count)
            {
                var memory = new ReadOnlyMemory<byte>(buffer);
                WriteAsync(memory.Slice(offset, count), default)
                    .AsTask().GetAwaiter().GetResult();
            }

            /// <inheritdoc/>
            public override Task CopyToAsync(Stream destination, int bufferSize,
                CancellationToken cancellationToken)
            {
                ValidateCopyToArguments(destination, bufferSize);
                ObjectDisposedException.ThrowIf(!_fileHandle.HasValue, this);

                if (!CanRead)
                {
                    throw new IOException("Cannot read from write-only stream");
                }

                bufferSize = Math.Min(bufferSize, (int)_bufferSize);
                return Core(this, destination, bufferSize, cancellationToken);

                static async Task Core(Stream source, Stream destination,
                    int bufferSize, CancellationToken cancellationToken)
                {
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                    try
                    {
                        int bytesRead;
                        while ((bytesRead = await source.ReadAsync(new Memory<byte>(buffer),
                            cancellationToken).ConfigureAwait(false)) != 0)
                        {
                            await destination.WriteAsync(
                                new ReadOnlyMemory<byte>(buffer, 0, bytesRead),
                                cancellationToken).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
            }

            /// <inheritdoc/>
            public override void CopyTo(Stream destination, int bufferSize)
            {
                CopyToAsync(destination, bufferSize, default).GetAwaiter().GetResult();
            }

            /// <inheritdoc/>
            public async ValueTask CloseAsync(CancellationToken cancellationToken)
            {
                ObjectDisposedException.ThrowIf(!_fileHandle.HasValue, this);
                var errorInfo = await _handle.Session.CloseAsync(
                    _header.ToRequestHeader(_outer._timeProvider), _nodeId,
                    _fileHandle.Value, cancellationToken).ConfigureAwait(false);
                if (errorInfo != null)
                {
                    throw new IOException(errorInfo.ErrorMessage);
                }

                // Closed - now release handle
                _handle.Dispose();
                _fileHandle = null;
            }

            /// <inheritdoc/>
            public override async ValueTask DisposeAsync()
            {
                if (_fileHandle.HasValue)
                {
                    try
                    {
                        await CloseAsync(default).ConfigureAwait(false);
                    }
                    catch { } // Best effort closing
                    finally
                    {
                        if (_fileHandle.HasValue)
                        {
                            _handle.Dispose();
                            _fileHandle = null; // Mark disposed
                        }
                    }
                }
                await base.DisposeAsync().ConfigureAwait(false);
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (disposing && _fileHandle.HasValue)
                {
                    DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                base.Dispose(disposing);
            }

            private readonly RequestHeaderModel _header;
            private readonly ISessionHandle _handle;
            private readonly NodeId _nodeId;
            private readonly FileSystemServices<T> _outer;
            private readonly FileInfoModel? _fileInfo;
            private readonly uint _bufferSize;
            private readonly FileWriteMode? _mode;
            private bool _isEoS;
            private uint? _fileHandle;
        }

        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
        private readonly INodeServicesInternal<T> _nodes;
        private readonly TimeProvider _timeProvider;
        private readonly IOpcUaClientManager<T> _client;
    }
}
