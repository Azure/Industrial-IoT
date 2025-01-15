// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Asset and Web of things connectivity
    /// </summary>
    public static class AssetsEx
    {
        /// <summary>
        /// Create asset
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="assetName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<(NodeId?, ServiceResultModel?)> CreateAssetAsync(
            this IOpcUaSession session, RequestHeader header, string assetName, CancellationToken ct)
        {
            var nsIndex = session.MessageContext.NamespaceUris.GetIndex(kNamespace);
            if (nsIndex < 0)
            {
                return (null, new ServiceResultModel
                {
                    StatusCode = StatusCodes.BadNotSupported,
                    ErrorMessage = "Namespace not found - asset connectivity not supported."
                });
            }
            try
            {
                // Call create method
                var request = new CallMethodRequestCollection
                {
                    new CallMethodRequest
                    {
                        ObjectId = new NodeId(kAsset_Root, (ushort)nsIndex),
                        MethodId = new NodeId(kAsset_CreateAsset, (ushort)nsIndex),
                        InputArguments = new [] { new Variant(assetName) }
                    }
                };
                var response = await session.Services.CallAsync(header, request,
                    ct).ConfigureAwait(false);
                var results = response.Validate(response.Results, r => r.StatusCode,
                    response.DiagnosticInfos, request);
                if (results.ErrorInfo != null)
                {
                    return (null, results.ErrorInfo);
                }
                if (results[0].ErrorInfo != null ||
                    results[0].Result?.OutputArguments == null ||
                    results[0].Result.OutputArguments.Count == 0 ||
                    results[0].Result.OutputArguments[0].Value is not NodeId assetNodeId)
                {
                    return (null, results[0].ErrorInfo ?? new ServiceResultModel
                    {
                        ErrorMessage = "no asset node id returned."
                    });
                }
                return (assetNodeId, null);
            }
            catch (Exception ex)
            {
                return (null, ex.ToServiceResultModel());
            }
        }

        /// <summary>
        /// Get the node id of the asset file under the asset
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="assetId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<(NodeId?, ServiceResultModel?)> GetAssetFileAsync(
            this IOpcUaSession session, RequestHeader header, NodeId assetId, CancellationToken ct)
        {
            var nsIndex = session.MessageContext.NamespaceUris.GetIndex(kNamespace);
            if (nsIndex < 0)
            {
                return (null, new ServiceResultModel
                {
                    StatusCode = StatusCodes.BadNotSupported,
                    ErrorMessage = "Namespace not found - asset connectivity not supported."
                });
            }

            var (results, errorInfo) = await session.FindAsync(header,
                assetId.YieldReturn(), ReferenceTypeIds.HasComponent, true,
                nodeClassMask: (uint)Opc.Ua.NodeClass.Object, ct: ct).ConfigureAwait(false);

            if (errorInfo != null)
            {
                return (null, errorInfo);
            }
            var fileNodeId = results
                .FirstOrDefault(f =>
                    f.ErrorInfo == null &&
                    f.TypeDefinition.NamespaceIndex == nsIndex &&
                    f.TypeDefinition.Identifier.Equals(kAssetFileType));
            if (!NodeId.IsNull(fileNodeId.Node))
            {
                return (fileNodeId.Node, null);
            }
            errorInfo = results.FirstOrDefault(d => d.ErrorInfo != null).ErrorInfo;
            errorInfo ??= new ServiceResultModel
            {
                StatusCode = StatusCodes.BadNotFound,
                ErrorMessage = "No file found for asset."
            };
            return (null, errorInfo);
        }

        /// <summary>
        /// Delete asset
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="assetId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<ServiceResultModel?> DeleteAssetAsync(this IOpcUaSession session,
            RequestHeader header, NodeId assetId, CancellationToken ct)
        {
            var nsIndex = session.MessageContext.NamespaceUris.GetIndex(kNamespace);
            if (nsIndex < 0)
            {
                return new ServiceResultModel
                {
                    StatusCode = StatusCodes.BadNotSupported,
                    ErrorMessage = "Namespace not found - asset connectivity not supported."
                };
            }
            try
            {
                // Call create method
                var request = new CallMethodRequestCollection
                {
                    new CallMethodRequest
                    {
                        ObjectId = new NodeId(kAsset_Root, (ushort)nsIndex),
                        MethodId = new NodeId(kAsset_DeleteAsset, (ushort)nsIndex),
                        InputArguments = new [] { new Variant(assetId) }
                    }
                };
                var response = await session.Services.CallAsync(header, request,
                    ct).ConfigureAwait(false);
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
        /// Close file handle of file and kick off the update
        /// </summary>
        /// <param name="session"></param>
        /// <param name="header"></param>
        /// <param name="fileNodeId"></param>
        /// <param name="fileHandle"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<ServiceResultModel?> CloseAndUpdateAsync(this IOpcUaSession session,
            RequestHeader header, NodeId fileNodeId, uint fileHandle, CancellationToken ct)
        {
            var nsIndex = session.MessageContext.NamespaceUris.GetIndex(kNamespace);
            if (nsIndex < 0)
            {
                return new ServiceResultModel
                {
                    StatusCode = StatusCodes.BadNotSupported,
                    ErrorMessage = "Namespace not found - asset connectivity not supported."
                };
            }
            return await session.CloseAsync(header, fileNodeId, new NodeId(
                kAssetFileType_CloseAndUpdate, (ushort)nsIndex), fileHandle, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get root asset node id
        /// </summary>
        public static string Root => $"nsu={kNamespace};i={kAsset_Root}";
        private const string kNamespace = "http://opcfoundation.org/UA/WoT-Con/";
        private const uint kAsset_Root = 31;
        private const uint kAsset_CreateAsset = 32;
        private const uint kAsset_DeleteAsset = 35;
        private const uint kAssetFileType = 110;
        private const uint kAssetFileType_CloseAndUpdate = 111;
    }
}
