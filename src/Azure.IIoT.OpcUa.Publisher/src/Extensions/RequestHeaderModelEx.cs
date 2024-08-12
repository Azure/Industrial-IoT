// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Extensions;

    /// <summary>
    /// Helpers for request header
    /// </summary>
    public static class RequestHeaderModelEx
    {
        /// <summary>
        /// Get namespace format for the response
        /// </summary>
        /// <param name="header"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static NamespaceFormat GetNamespaceFormat(this RequestHeaderModel? header,
            IOptions<PublisherOptions>? options = null)
        {
            return header?.NamespaceFormat ?? options?.Value.DefaultNamespaceFormat
                ?? NamespaceFormat.Uri;
        }

        /// <summary>
        /// Convert to string based on request header
        /// </summary>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string AsString(this RequestHeaderModel? header, NodeId nodeId,
            IServiceMessageContext context, IOptions<PublisherOptions>? options = null)
        {
            return nodeId.AsString(context, header.GetNamespaceFormat(options)) ?? string.Empty;
        }

        /// <summary>
        /// Convert to string based on request header
        /// </summary>
        /// <param name="header"></param>
        /// <param name="nodeId"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string AsString(this RequestHeaderModel? header, ExpandedNodeId nodeId,
            IServiceMessageContext context, IOptions<PublisherOptions>? options = null)
        {
            return nodeId.AsString(context, header.GetNamespaceFormat(options)) ?? string.Empty;
        }

        /// <summary>
        /// Convert to string based on request header
        /// </summary>
        /// <param name="header"></param>
        /// <param name="qualifiedName"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string AsString(this RequestHeaderModel? header, QualifiedName qualifiedName,
            IServiceMessageContext context, IOptions<PublisherOptions>? options = null)
        {
            return qualifiedName.AsString(context, header.GetNamespaceFormat(options)) ?? string.Empty;
        }
    }
}
