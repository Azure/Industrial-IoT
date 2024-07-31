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

    /// <summary>
    /// This class provides access to a servers address space providing
    /// Filesystem services. It uses the OPC ua client interface to access
    /// the server.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class FileSystemServices<T> : IFileSystemServices<T>,
        IDisposable
    {
        /// <summary>
        /// Create node service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        /// <param name="nodes"></param>
        /// <param name="timeProvider"></param>
        public FileSystemServices(IOpcUaClientManager<T> client,
            ILogger<NodeServices<T>> logger, IOptions<PublisherOptions> options,
            INodeServicesInternal<T> nodes, TimeProvider? timeProvider = null)
        {
            _logger = logger;
            _client = client;
            _options = options;
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
        public async IAsyncEnumerable<ServiceResponse<FileSystemObjectModel>> GetDirectoriesAsync(
            T endpoint, FileSystemObjectModel fileSystemOrDirectory,
            [EnumeratorCancellation] CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("GetDirectories");
            var header = new RequestHeaderModel();
            await Task.Delay(0, ct).ConfigureAwait(false);
            yield break;
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<ServiceResponse<FileSystemObjectModel>> GetFilesAsync(
            T endpoint, FileSystemObjectModel fileSystemOrDirectory,
            [EnumeratorCancellation] CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("GetFiles");
            var header = new RequestHeaderModel();
            await Task.Delay(0, ct).ConfigureAwait(false);
            yield break;
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<Stream>> OpenReadAsync(T endpoint,
            FileSystemObjectModel file, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("Read");
            var header = new RequestHeaderModel();
            await Task.Delay(0, ct).ConfigureAwait(false);
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<Stream>> OpenWriteAsync(T endpoint,
            FileSystemObjectModel file, FileWriteMode mode, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("OpenWrite");
            var header = new RequestHeaderModel();
            await Task.Delay(0, ct).ConfigureAwait(false);
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<ServiceResultModel> CopyToAsync(T endpoint, FileSystemObjectModel file,
            Stream stream, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("AppendTo");
            var header = new RequestHeaderModel();
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var nodeId = await GetFileSystemNodeIdAsync(context.Session, header,
                    file, context.Ct).ConfigureAwait(false);

                var fileInfo = await GetFileInfoAsync(endpoint, file, ct).ConfigureAwait(false);
                if (fileInfo.ErrorInfo != null)
                {
                    return fileInfo.ErrorInfo;
                }
                var bufferSize = fileInfo.Result?.MaxBufferSize;
                if (bufferSize == null)
                {
                    var caps = await context.Session.GetServerCapabilitiesAsync(
                        NamespaceFormat.Index, context.Ct).ConfigureAwait(false);
                    bufferSize = caps.OperationLimits.MaxByteStringLength;
                }

                // Open for reading
                var (fileHandle, errorInfo) = await OpenAsync(context.Session, header,
                    nodeId, 0x1, context.Ct).ConfigureAwait(false);
                if (errorInfo != null)
                {
                    return errorInfo;
                }
                Debug.Assert(fileHandle.HasValue);
                try
                {
                    while (!context.Ct.IsCancellationRequested)
                    {
                        var (buffer, readError) = await ReadAsync(context.Session,
                            header, nodeId, fileHandle.Value, (int)(bufferSize ?? 4096),
                            context.Ct).ConfigureAwait(false);
                        if (errorInfo != null)
                        {
                            return errorInfo;
                        }
                        Debug.Assert(buffer != null);
                        if (buffer.Length == 0)
                        {
                            // end of stream
                            break;
                        }
                        await stream.WriteAsync(buffer, ct).ConfigureAwait(false);
                    }
                    await stream.FlushAsync(ct).ConfigureAwait(false);
                    return new ServiceResultModel();
                }
                catch (Exception ex)
                {
                    return ex.ToServiceResultModel();
                }
                finally
                {
                    await CloseAsync(context.Session, header, nodeId,
                        fileHandle.Value, context.Ct).ConfigureAwait(false);
                }
            }, header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResultModel> CopyFromAsync(T endpoint, FileSystemObjectModel file,
            Stream stream, FileWriteMode mode, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("AppendTo");
            var header = new RequestHeaderModel();
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var nodeId = await GetFileSystemNodeIdAsync(context.Session,
                    header, file, context.Ct).ConfigureAwait(false);

                var fileInfo = await GetFileInfoAsync(endpoint, file, ct).ConfigureAwait(false);
                if (fileInfo.ErrorInfo != null)
                {
                    return fileInfo.ErrorInfo;
                }
                var bufferSize = fileInfo.Result?.MaxBufferSize;
                if (bufferSize == null)
                {
                    var caps = await context.Session.GetServerCapabilitiesAsync(
                        NamespaceFormat.Index, context.Ct).ConfigureAwait(false);
                    bufferSize = caps.OperationLimits.MaxByteStringLength;
                }

                // Open
                var (fileHandle, errorInfo) = await OpenAsync(context.Session, header,
                    nodeId, mode switch
                    {
                        FileWriteMode.Create => 0x6, // Write bit plus erase
                        FileWriteMode.Append => 0x10, // Write bit plus append
                        _ => 0x2 // Write bit - use 0x1 for reading
                    }, context.Ct).ConfigureAwait(false);
                if (errorInfo != null)
                {
                    return errorInfo;
                }
                Debug.Assert(fileHandle.HasValue);
                try
                {
                    var buffer = new Memory<byte>(new byte[bufferSize ?? 4096]);
                    while (!context.Ct.IsCancellationRequested)
                    {
                        var bytesRead = await stream.ReadAsync(buffer,
                            context.Ct).ConfigureAwait(false);
                        if (bytesRead == 0)
                        {
                            break;
                        }
                        errorInfo = await WriteAsync(context.Session, header, nodeId,
                            fileHandle.Value, buffer.Slice(0, bytesRead).ToArray(),
                            context.Ct).ConfigureAwait(false);
                        if (errorInfo != null)
                        {
                            return errorInfo;
                        }
                    }
                }
                finally
                {
                    await CloseAsync(context.Session, header, nodeId,
                        fileHandle.Value, context.Ct).ConfigureAwait(false);
                }
                return new ServiceResultModel();
            }, header, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<FileSystemObjectModel>> CreateDirectoryAsync(T endpoint,
            FileSystemObjectModel fileSystemOrDirectory, string name, CancellationToken ct)
        {
            using var trace = _activitySource.StartActivity("CreateDirectory");
            var header = new RequestHeaderModel();
            return await _client.ExecuteAsync(endpoint, async context =>
            {
                var nodeId = await GetFileSystemNodeIdAsync(context.Session,
                    header, fileSystemOrDirectory, context.Ct).ConfigureAwait(false);

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
                var nodeId = await GetFileSystemNodeIdAsync(context.Session,
                    header, fileSystemOrDirectory, context.Ct).ConfigureAwait(false);

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
                var nodeId = await GetFileSystemNodeIdAsync(context.Session,
                    header, parentOrObjectToDelete, context.Ct).ConfigureAwait(false);

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
                var nodeId = await GetFileSystemNodeIdAsync(context.Session,
                    header, file, context.Ct).ConfigureAwait(false);
                var browsePaths = new string[]
                {
                    BrowseNames.Size,
                    BrowseNames.Writable,
                    BrowseNames.UserWritable,
                    BrowseNames.OpenCount,
                    BrowseNames.MimeType,
                    BrowseNames.MaxByteStringLength,
                    BrowseNames.LastModifiedTime
                };
                var response =
                    await context.Session.Services.TranslateBrowsePathsToNodeIdsAsync(
                        header.ToRequestHeader(_timeProvider), browsePaths.Select(b => new BrowsePath
                        {
                            StartingNode = nodeId,
                            RelativePath = new RelativePath(b)
                        }).ToArray(), ct).ConfigureAwait(false);
                Debug.Assert(response != null);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, browsePaths);
                if (results.ErrorInfo != null)
                {
                    return new ServiceResponse<FileInfoModel> { ErrorInfo = results.ErrorInfo };
                }

                var read = await context.Session.Services.ReadAsync(
                    header.ToRequestHeader(_timeProvider), 0.0, Opc.Ua.TimestampsToReturn.Neither,
                    results.Select(r => new ReadValueId
                    {
                        AttributeId = Attributes.Value,
                        NodeId = r.Result.Targets[0].TargetId
                            .ToNodeId(context.Session.MessageContext.NamespaceUris)
                    }).ToArray(), ct).ConfigureAwait(false);
                var values = read.Validate(read.Results, r => r.StatusCode, read.DiagnosticInfos,
                    browsePaths);
                if (values.ErrorInfo != null)
                {
                    return new ServiceResponse<FileInfoModel> { ErrorInfo = values.ErrorInfo };
                }
                return new ServiceResponse<FileInfoModel>
                {
                    Result = new FileInfoModel
                    {
                        Size = (values[0].Result?.Value as long?) ?? 0,
                        Writable = (values[2].Result?.Value as bool?) ?? false,
                        OpenCount = (values[3].Result?.Value as int?) ?? 0,
                        MimeType = (values[4].Result?.Value as string),
                        MaxBufferSize = (values[5].Result?.Value as uint?),
                        LastModified = (values[6].Result?.Value as DateTime?)
                    }
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
        private async Task<NodeId> GetFileSystemNodeIdAsync(IOpcUaSession session,
            RequestHeaderModel header, FileSystemObjectModel fileSystemObject, CancellationToken ct)
        {
            var nodeId = fileSystemObject.NodeId.ToNodeId(session.MessageContext);
            if (fileSystemObject.BrowsePath?.Count > 0)
            {
                nodeId = await ResolveBrowsePathToNodeAsync(session, header,
                nodeId, fileSystemObject.BrowsePath.ToArray(), nameof(fileSystemObject.BrowsePath),
                    _timeProvider, ct).ConfigureAwait(false);
            }
            if (NodeId.IsNull(nodeId))
            {
                throw new ArgumentException("Node id missing", nameof(fileSystemObject));
            }
            return nodeId;
        }

        /// <summary>
        /// Open file
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="mode"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<(uint?, ServiceResultModel?)> OpenAsync(IOpcUaSession session,
            RequestHeaderModel header, NodeId nodeId, byte mode, CancellationToken ct)
        {
            try
            {
                // Call open method
                var request = new CallMethodRequestCollection
                {
                    new CallMethodRequest
                    {
                        ObjectId = nodeId,
                        MethodId = Opc.Ua.MethodIds.FileType_Open,
                        InputArguments = new [] { new Variant(mode) }
                    }
                };
                var response = await session.Services.CallAsync(header
                    .ToRequestHeader(_timeProvider), request, ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, request);
                if (results.ErrorInfo != null)
                {
                    return (null, results.ErrorInfo);
                }
                if (results[0].Result?.OutputArguments == null ||
                    results[0].Result.OutputArguments.Count == 0 ||
                    results[0].Result.OutputArguments[0].Value is not uint fileHandle)
                {
                    return (null, new ServiceResultModel
                    {
                        ErrorMessage = "no file handle returned"
                    });
                }
                return (fileHandle, null);
            }
            catch (Exception ex)
            {
                return (null, ex.ToServiceResultModel());
            }
        }

        /// <summary>
        /// Write buffer at current position in file
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="fileHandle"></param>
        /// <param name="buffer"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<ServiceResultModel?> WriteAsync(IOpcUaSession session,
            RequestHeaderModel header, NodeId nodeId, uint fileHandle, byte[] buffer,
            CancellationToken ct)
        {
            try
            {
                // Call write method
                var request = new CallMethodRequestCollection
                {
                    new CallMethodRequest
                    {
                        ObjectId = nodeId,
                        MethodId = Opc.Ua.MethodIds.FileType_Write,
                        InputArguments = new [] { new Variant(fileHandle), new Variant(buffer) }
                    }
                };
                var response = await session.Services.CallAsync(header
                    .ToRequestHeader(_timeProvider), request, ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, request);
                return results.ErrorInfo;
            }
            catch (Exception ex)
            {
                return ex.ToServiceResultModel();
            }
        }

        /// <summary>
        /// Read from current position in file
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="fileHandle"></param>
        /// <param name="length"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<(byte[]?, ServiceResultModel?)> ReadAsync(IOpcUaSession session,
            RequestHeaderModel header, NodeId nodeId, uint fileHandle, int length,
            CancellationToken ct)
        {
            try
            {
                // Call write method
                var request = new CallMethodRequestCollection
                {
                    new CallMethodRequest
                    {
                        ObjectId = nodeId,
                        MethodId = Opc.Ua.MethodIds.FileType_Read,
                        InputArguments = new [] { new Variant(fileHandle), new Variant(length) }
                    }
                };
                var response = await session.Services.CallAsync(header
                    .ToRequestHeader(_timeProvider), request, ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, request);
                if (results.ErrorInfo != null)
                {
                    return (null, results.ErrorInfo);
                }
                if (results[0].Result?.OutputArguments == null ||
                    results[0].Result.OutputArguments.Count == 0 ||
                    results[0].Result.OutputArguments[0].Value is not byte[] byteString)
                {
                    byteString = Array.Empty<byte>();
                }
                return (byteString, null);
            }
            catch (Exception ex)
            {
                return (null, ex.ToServiceResultModel());
            }
        }

        /// <summary>
        /// Close file handle of file
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="fileHandle"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<ServiceResultModel?> CloseAsync(IOpcUaSession session,
            RequestHeaderModel header, NodeId nodeId, uint fileHandle, CancellationToken ct)
        {
            // Call open method
            try
            {
                var request = new CallMethodRequestCollection
                {
                    new CallMethodRequest
                    {
                        ObjectId = nodeId,
                        MethodId = Opc.Ua.MethodIds.FileType_Close,
                        InputArguments = new [] { new Variant(fileHandle) }
                    }
                };
                var response = await session.Services.CallAsync(header
                    .ToRequestHeader(_timeProvider), request, ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, request);
                return results.ErrorInfo;
            }
            catch (Exception ex)
            {
                return ex.ToServiceResultModel();
            }
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
        /// <param name="qualifiedName"></param>
        /// <param name="context"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        private string AsString(QualifiedName qualifiedName,
            IServiceMessageContext context, RequestHeaderModel? header)
        {
            return qualifiedName.AsString(context, _nodes.GetNamespaceFormat(header)) ?? string.Empty;
        }

        /// <summary>
        /// File transfer stream
        /// </summary>
        private class FileTransferStream : Stream
        {
            /// <inheritdoc/>
            public override bool CanRead { get; }
            /// <inheritdoc/>
            public override bool CanSeek { get; }
            /// <inheritdoc/>
            public override bool CanWrite { get; }
            /// <inheritdoc/>
            public override long Length { get; }
            /// <inheritdoc/>
            public override long Position { get; set; }

            /// <inheritdoc/>
            public override bool CanTimeout => base.CanTimeout;

            /// <inheritdoc/>
            public override int ReadTimeout { get => base.ReadTimeout; set => base.ReadTimeout = value; }
            /// <inheritdoc/>
            public override int WriteTimeout { get => base.WriteTimeout; set => base.WriteTimeout = value; }

            public FileTransferStream(FileSystemServices<T> outer)
            {

            }

            /// <inheritdoc/>
            public override void Flush() { }

            /// <inheritdoc/>
            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override void Close()
            {
                base.Close();
            }

            /// <inheritdoc/>
            public override void CopyTo(Stream destination, int bufferSize)
            {
                base.CopyTo(destination, bufferSize);
            }

            /// <inheritdoc/>
            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                return base.CopyToAsync(destination, bufferSize, cancellationToken);
            }

            /// <inheritdoc/>
            public override ValueTask DisposeAsync()
            {
                return base.DisposeAsync();
            }

            /// <inheritdoc/>
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }

            /// <inheritdoc/>
            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }

        private readonly ActivitySource _activitySource = Diagnostics.NewActivitySource();
        private readonly ILogger _logger;
        private readonly IOptions<PublisherOptions> _options;
        private readonly INodeServicesInternal<T> _nodes;
        private readonly TimeProvider _timeProvider;
        private readonly IOpcUaClientManager<T> _client;
    }
}
