// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Extensions;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Azure.Amqp.Framing;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Export;
    using Opc.Ua.Extensions;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class provides foundational file transfer services.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class FileSystemServices<T> : IFileSystemServices<T>, IDisposable
    {
        /// <summary>
        /// Create node service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="options"></param>
        /// <param name="timeProvider"></param>
        public FileSystemServices(IOpcUaClientManager<T> client,
            IOptions<PublisherOptions> options, TimeProvider? timeProvider = null)
        {
            _client = client;
            _options = options;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _activitySource.Dispose();
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ServiceResponse<FileSystemObjectModel>> GetFileSystemsAsync(
            T endpoint, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("GetFileSystems");
            var header = new RequestHeaderModel();

            var browser = new FileSystemBrowser(header, _options, _timeProvider);
            return _client.ExecuteAsync(endpoint, browser, header, ct);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<IEnumerable<FileSystemObjectModel>>> GetDirectoriesAsync(
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
                    return new ServiceResponse<IEnumerable<FileSystemObjectModel>>
                    {
                        ErrorInfo = argInfo
                    };
                }
                var (references, errorInfo) = await context.Session.FindAsync(
                    header.ToRequestHeader(_timeProvider), nodeId.YieldReturn(),
                    ReferenceTypeIds.HasComponent, ct: context.Ct).ConfigureAwait(false);
                if (errorInfo == null && references.Count > 0 &&
                    references.All(r => r.ErrorInfo != null))
                {
                    errorInfo = references[0].ErrorInfo;
                }
                return new ServiceResponse<IEnumerable<FileSystemObjectModel>>
                {
                    ErrorInfo = errorInfo,
                    Result = references
                        .Where(r => r.TypeDefinition == Opc.Ua.ObjectTypes.FileDirectoryType &&
                            r.ErrorInfo == null)
                        .Select(f => new FileSystemObjectModel
                        {
                            NodeId = header.AsString(f.Node, context.Session.MessageContext, _options),
                            Name = f.DisplayName
                        })
                };
            }, header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<IEnumerable<FileSystemObjectModel>>> GetFilesAsync(
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
                    return new ServiceResponse<IEnumerable<FileSystemObjectModel>>
                    {
                        ErrorInfo = argInfo
                    };
                }

                var (references, errorInfo) = await context.Session.FindAsync(
                    header.ToRequestHeader(_timeProvider), nodeId.YieldReturn(),
                    ReferenceTypeIds.HasComponent, ct: context.Ct).ConfigureAwait(false);
                if (errorInfo == null && references.Count > 0 &&
                    references.All(r => r.ErrorInfo != null))
                {
                    errorInfo = references[0].ErrorInfo;
                }
                return new ServiceResponse<IEnumerable<FileSystemObjectModel>>
                {
                    ErrorInfo = errorInfo,
                    Result = references
                        .Where(r => r.TypeDefinition == Opc.Ua.ObjectTypes.FileType &&
                            r.ErrorInfo == null)
                        .Select(f => new FileSystemObjectModel
                        {
                            NodeId = header.AsString(f.Node, context.Session.MessageContext, _options),
                            Name = f.DisplayName
                        })
                };
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
            FileSystemObjectModel file, FileOpenWriteOptionsModel? options, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("OpenWrite");
            var header = new RequestHeaderModel();
            var (stream, errorInfo) = await FileTransferStream.OpenAsync(this,
                endpoint, header, file, options ?? new FileOpenWriteOptionsModel(),
                ct).ConfigureAwait(false);
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
                        InputArguments = new [] { new Variant(name) }
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
                if (results[0].ErrorInfo != null ||
                    results[0].Result?.OutputArguments == null ||
                    results[0].Result.OutputArguments.Count == 0 ||
                    results[0].Result.OutputArguments[0].Value is not NodeId result)
                {
                    return new ServiceResponse<FileSystemObjectModel>
                    {
                        ErrorInfo = results[0].ErrorInfo ??
                            new ServiceResultModel { ErrorMessage = "no node id returned" }
                    };
                }
                return new ServiceResponse<FileSystemObjectModel>
                {
                    Result = new FileSystemObjectModel
                    {
                        NodeId = header.AsString(result, context.Session.MessageContext, _options),
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
                if (results[0].ErrorInfo != null ||
                    results[0].Result?.OutputArguments == null ||
                    results[0].Result.OutputArguments.Count == 0 ||
                    results[0].Result.OutputArguments[0].Value is not NodeId result)
                {
                    return new ServiceResponse<FileSystemObjectModel>
                    {
                        ErrorInfo = results[0].ErrorInfo ??
                            new ServiceResultModel { ErrorMessage = "no node id returned" }
                    };
                }
                return new ServiceResponse<FileSystemObjectModel>
                {
                    Result = new FileSystemObjectModel
                    {
                        NodeId = header.AsString(result, context.Session.MessageContext, _options),
                        Name = name
                    }
                };
            }, header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<FileSystemObjectModel>> GetParentAsync(T endpoint,
            FileSystemObjectModel fileOrDirectoryObject, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("GetParent");
            var header = new RequestHeaderModel();
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var (nodeId, argInfo) = await GetFileSystemNodeIdAsync(context.Session,
                     header, fileOrDirectoryObject, context.Ct).ConfigureAwait(false);
                if (argInfo != null)
                {
                    return new ServiceResponse<FileSystemObjectModel> { ErrorInfo = argInfo };
                }

                // Find parent
                var (parents, argInfo2) = await context.Session.FindAsync(
                    header.ToRequestHeader(_timeProvider), nodeId.YieldReturn(),
                    ReferenceTypeIds.HasComponent, isInverse: true,
                    maxResults: 1, ct: context.Ct).ConfigureAwait(false);
                if (argInfo2 != null)
                {
                    return new ServiceResponse<FileSystemObjectModel> { ErrorInfo = argInfo2 };
                }
                var result = parents.Count > 0 ? parents[0] : default;
                nodeId = result.Node;
                if (NodeId.IsNull(nodeId) ||
                    result.TypeDefinition != Opc.Ua.ObjectTypeIds.FileDirectoryType)
                {
                    return new ServiceResponse<FileSystemObjectModel>
                    {
                        ErrorInfo = new ServiceResultModel
                        {
                            StatusCode = StatusCodes.BadNodeIdInvalid,
                            ErrorMessage = "Could not find a file directory object parent."
                        }
                    };
                }
                return new ServiceResponse<FileSystemObjectModel>
                {
                    Result = new FileSystemObjectModel
                    {
                        NodeId = header.AsString(nodeId, context.Session.MessageContext, _options),
                        Name = result.DisplayName
                    }
                };
            }, header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResultModel> DeleteFileSystemObjectAsync(T endpoint,
            FileSystemObjectModel fileOrDirectoryObject, FileSystemObjectModel? parentFileSystemOrDirectory,
            CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("DeleteFileSystemObject");
            var header = new RequestHeaderModel();
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var (nodeId, argInfo) = await GetFileSystemNodeIdAsync(context.Session,
                     header, fileOrDirectoryObject, context.Ct).ConfigureAwait(false);
                if (argInfo != null)
                {
                    return argInfo;
                }

                var targetId = nodeId;
                if (parentFileSystemOrDirectory != null)
                {
                    (nodeId, argInfo) = await GetFileSystemNodeIdAsync(context.Session,
                         header, parentFileSystemOrDirectory, context.Ct).ConfigureAwait(false);
                    if (argInfo != null)
                    {
                        return argInfo;
                    }
                }
                else
                {
                    // Find parent
                    var (parents, argInfo2) = await context.Session.FindAsync(
                        header.ToRequestHeader(_timeProvider), targetId.YieldReturn(),
                        ReferenceTypeIds.HasComponent, isInverse: true,
                        maxResults: 1, ct: context.Ct).ConfigureAwait(false);
                    if (argInfo2 != null)
                    {
                        return argInfo2;
                    }
                    var result = parents.Count > 0 ? parents[0] : default;
                    nodeId = result.Node;
                    if (NodeId.IsNull(nodeId) ||
                        result.TypeDefinition != Opc.Ua.ObjectTypeIds.FileDirectoryType)
                    {
                        return new ServiceResultModel
                        {
                            StatusCode = StatusCodes.BadNodeIdInvalid,
                            ErrorMessage = "Could not find a file directory object parent."
                        };
                    }
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
                return results.ErrorInfo ?? results[0].ErrorInfo ?? new ServiceResultModel();
            }, header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<FileInfoModel>> GetFileInfoAsync(T endpoint,
            FileSystemObjectModel file, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("GetFileInfo");
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
                nodeId ??= ObjectIds.RootFolder;
                try
                {
                    nodeId = await session.ResolveBrowsePathToNodeAsync(header,
                        nodeId, fileSystemObject.BrowsePath.Select(b => "<HasComponent>" + b).ToArray(),
                        nameof(fileSystemObject.BrowsePath), _timeProvider, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    return (NodeId.Null, ex.ToServiceResultModel());
                }
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
        /// File system object browser browses from root all objects of file directory type.
        /// The browse operation stops at the first found and returns the result.
        /// </summary>
        private sealed class FileSystemBrowser : AsyncEnumerableBrowser<ServiceResponse<FileSystemObjectModel>>
        {
            /// <inheritdoc/>
            public FileSystemBrowser(RequestHeaderModel? header, IOptions<PublisherOptions> options,
                TimeProvider timeProvider) : base(header, options, timeProvider,
                    null, ObjectTypeIds.FileDirectoryType)
            {
            }

            /// <inheritdoc/>
            protected override IEnumerable<ServiceResponse<FileSystemObjectModel>> HandleError(
                ServiceCallContext context, ServiceResultModel errorInfo)
            {
                yield return new ServiceResponse<FileSystemObjectModel>
                {
                    ErrorInfo = errorInfo
                };
            }

            /// <inheritdoc/>
            protected override IEnumerable<ServiceResponse<FileSystemObjectModel>> HandleMatching(
                ServiceCallContext context, IReadOnlyList<BrowseFrame> matching,
                List<ReferenceDescription> references)
            {
                // Only add what we did not match to browse deeper
                var stop = matching.Select(r => r.NodeId).ToHashSet();
                references.RemoveAll(r => stop.Contains((NodeId)r.NodeId));
                return matching.Select(match => new ServiceResponse<FileSystemObjectModel>
                {
                    Result = new FileSystemObjectModel
                    {
                        NodeId = Header.AsString(match.NodeId,
                            context.Session.MessageContext, Options),
                        Name = match.DisplayName
                    }
                });
            }
        }

        /// <summary>
        /// File transfer stream
        /// </summary>
        private sealed class FileTransferStream : Stream
        {
            /// <inheritdoc/>
            public override bool CanRead
                => _options == null && _fileHandle.HasValue;

            /// <inheritdoc/>
            public override bool CanWrite
                => _options != null && _fileHandle.HasValue;

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
            /// <param name="options"></param>
            public FileTransferStream(FileSystemServices<T> outer,
                ISessionHandle handle, RequestHeaderModel header,
                NodeId nodeId, uint fileHandle, FileInfoModel? fileInfo,
                uint bufferSize, FileOpenWriteOptionsModel? options = null)
            {
                _handle = handle;
                _nodeId = nodeId;
                _fileHandle = fileHandle;
                _outer = outer;
                _header = header;
                _fileInfo = fileInfo;
                _bufferSize = bufferSize;
                _options = options;

                if (options?.Mode == FileWriteMode.Append)
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
            /// <param name="options"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public static async Task<(Stream?, ServiceResultModel?)> OpenAsync(
                FileSystemServices<T> outer, T endpoint, RequestHeaderModel header,
                FileSystemObjectModel file, FileOpenWriteOptionsModel? options = null,
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

                    var tryCreate = errorInfo != null;
                    if (errorInfo != null)
                    {
                        // There should be file info
                        return (null, errorInfo);
                    }
                    if (options != null && fileInfo?.Writable == false)
                    {
                        return (null, new ServiceResultModel
                        {
                            StatusCode = StatusCodes.BadNotWritable,
                            ErrorMessage = "File is not writable."
                        });
                    }

                    var bufferSize = fileInfo?.MaxBufferSize;
                    if (bufferSize == null || bufferSize == 0)
                    {
                        var caps = await handle.Session.GetOperationLimitsAsync(
                            ct).ConfigureAwait(false);
                        bufferSize = caps.MaxByteStringLength;
                        if (bufferSize == null || bufferSize == 0)
                        {
                            bufferSize = 4096;
                        }
                    }

                    var (fileHandle, errorInfo2) = await handle.Session.OpenAsync(
                        header.ToRequestHeader(outer._timeProvider), nodeId, options?.Mode switch
                        {
                            FileWriteMode.Create => 0x2 | 0x4, // Write bit plus erase
                            FileWriteMode.Append => 0x2 | 0x8, // Write bit plus append
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
                        fileHandle.Value, fileInfo, bufferSize.Value, options), null);
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
                    buffer = buffer[readCount..];
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
                        _fileHandle.Value, buffer[..writeCount].ToArray(),
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
                    buffer = buffer[writeCount..];
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
                    var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
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
            public ValueTask CloseAsync(CancellationToken cancellationToken)
            {
                var alt = _options?.CloseAndCommitMethodId?
                    .ToNodeId(_handle.Session.MessageContext);
                return CloseAsync(alt, cancellationToken);
            }

            /// <summary>
            /// Close with alternative method
            /// </summary>
            /// <param name="alt"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            /// <exception cref="IOException"></exception>
            private async ValueTask CloseAsync(NodeId? alt, CancellationToken ct)
            {
                ObjectDisposedException.ThrowIf(!_fileHandle.HasValue, this);
                var errorInfo = await _handle.Session.CloseAsync(
                    _header.ToRequestHeader(_outer._timeProvider), _nodeId, alt,
                    _fileHandle.Value, ct).ConfigureAwait(false);
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
                        await CloseAsync(null, default).ConfigureAwait(false);
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
            private readonly FileOpenWriteOptionsModel? _options;
            private bool _isEoS;
            private uint? _fileHandle;
        }

        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
        private readonly IOptions<PublisherOptions> _options;
        private readonly TimeProvider _timeProvider;
        private readonly IOpcUaClientManager<T> _client;
    }
}
