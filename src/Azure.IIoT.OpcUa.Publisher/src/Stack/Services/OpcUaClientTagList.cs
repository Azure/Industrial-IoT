// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Connection metric tags
    /// </summary>
    internal sealed class OpcUaClientTagList : IMetricsContext
    {
        /// <inheritdoc/>
        public TagList TagList { get; }

        /// <summary>
        /// Parent context
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="parent"></param>
        public OpcUaClientTagList(ConnectionModel connection, IMetricsContext? parent = null)
        {
            if (connection.Endpoint == null)
            {
                throw new ArgumentException("Missing endpoint", nameof(connection));
            }
            var existing = parent?.TagList ?? default;
            var tags = existing.ToDictionary(kv => kv.Key, kv => kv.Value);
            tags.AddOrUpdate("endpointUrl", connection.Endpoint.Url);
            tags.AddOrUpdate("securityMode", connection.Endpoint.SecurityMode);
            if (connection.Group != null && !tags.ContainsKey(Constants.WriterGroupIdTag))
            {
                tags.Add(Constants.WriterGroupIdTag, connection.Group);
            }
            TagList = new TagList(tags.ToArray().AsSpan());
        }
    }
}
