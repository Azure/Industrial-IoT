// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Buffers;

    /// <summary>
    /// File system methods
    /// </summary>
    public static class FileSystemEx
    {
        /// <summary>
        /// Get file info
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<(FileInfoModel?, ServiceResultModel?)> GetFileInfoAsync(
            this IOpcUaSession session, RequestHeader header, NodeId nodeId,
            CancellationToken ct = default)
        {
            try
            {
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
                var response = await session.Services.TranslateBrowsePathsToNodeIdsAsync(
                    header, browsePaths.Select(b => new BrowsePath
                    {
                        StartingNode = nodeId,
                        RelativePath = new RelativePath(b)
                    }).ToArray(), ct).ConfigureAwait(false);
                Debug.Assert(response != null);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, browsePaths);
                if (results.ErrorInfo != null)
                {
                    return (null, results.ErrorInfo);
                }

                var read = await session.Services.ReadAsync(header, 0.0,
                    Opc.Ua.TimestampsToReturn.Neither, results.Select(r => new ReadValueId
                    {
                        AttributeId = Attributes.Value,
                        NodeId = r.Result.Targets[0].TargetId
                            .ToNodeId(session.MessageContext.NamespaceUris)
                    }).ToArray(), ct).ConfigureAwait(false);
                var values = read.Validate(read.Results, r => r.StatusCode, read.DiagnosticInfos,
                    browsePaths);
                if (values.ErrorInfo != null)
                {
                    return (null, results.ErrorInfo);
                }
                return (new FileInfoModel
                {
                    Size = values[0].Result?.Value as long? ?? 0,
                    Writable = values[2].Result?.Value as bool? ?? false,
                    OpenCount = values[3].Result?.Value as int? ?? 0,
                    MimeType = values[4].Result?.Value as string,
                    MaxBufferSize = values[5].Result?.Value as uint?,
                    LastModified = values[6].Result?.Value as DateTime?
                }, null);
            }
            catch (Exception ex)
            {
                return (null, ex.ToServiceResultModel());
            }
        }

        /// <summary>
        /// Get buffer size
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<uint?> GetBufferSizeAsync(this IOpcUaSession session,
            RequestHeader header, NodeId nodeId, CancellationToken ct = default)
        {
            try
            {
                var (fileInfo, errorInfo) = await session.GetFileInfoAsync(
                    header, nodeId, ct).ConfigureAwait(false);
                var bufferSize = fileInfo?.MaxBufferSize;
                if (errorInfo == null && fileInfo?.MaxBufferSize != null)
                {
                    return fileInfo.MaxBufferSize;
                }
                var caps = await session.GetServerCapabilitiesAsync(
                    NamespaceFormat.Index, ct).ConfigureAwait(false);
                return caps.OperationLimits.MaxByteStringLength;
            }
            catch
            {
                return null;
            }
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
        public static async Task<(uint?, ServiceResultModel?)> OpenAsync(this IOpcUaSession session,
            RequestHeader header, NodeId nodeId, byte mode, CancellationToken ct)
        {
            try
            {
                // Call open method
                var request = new CallMethodRequestCollection
                {
                    new CallMethodRequest
                    {
                        ObjectId = nodeId,
                        MethodId = MethodIds.FileType_Open,
                        InputArguments = new [] { new Variant(mode) }
                    }
                };
                var response = await session.Services.CallAsync(header, request, ct).ConfigureAwait(false);
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
        public static async Task<ServiceResultModel?> WriteAsync(this IOpcUaSession session,
            RequestHeader header, NodeId nodeId, uint fileHandle, byte[] buffer,
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
                        MethodId = MethodIds.FileType_Write,
                        InputArguments = new [] { new Variant(fileHandle), new Variant(buffer) }
                    }
                };
                var response = await session.Services.CallAsync(header, request, ct).ConfigureAwait(false);
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
        public static async Task<(byte[]?, ServiceResultModel?)> ReadAsync(this IOpcUaSession session,
            RequestHeader header, NodeId nodeId, uint fileHandle, int length,
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
                        MethodId = MethodIds.FileType_Read,
                        InputArguments = new [] { new Variant(fileHandle), new Variant(length) }
                    }
                };
                var response = await session.Services.CallAsync(header, request, ct).ConfigureAwait(false);
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
        public static async Task<ServiceResultModel?> CloseAsync(this IOpcUaSession session,
            RequestHeader header, NodeId nodeId, uint fileHandle, CancellationToken ct)
        {
            // Call open method
            try
            {
                var request = new CallMethodRequestCollection
                {
                    new CallMethodRequest
                    {
                        ObjectId = nodeId,
                        MethodId = MethodIds.FileType_Close,
                        InputArguments = new [] { new Variant(fileHandle) }
                    }
                };
                var response = await session.Services.CallAsync(header, request, ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, request);
                return results.ErrorInfo;
            }
            catch (Exception ex)
            {
                return ex.ToServiceResultModel();
            }
        }
    }
}
