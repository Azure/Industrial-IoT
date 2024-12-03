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

            TagList = new TagList([.. tags])
            {
                 new KeyValuePair<string, object?>("endpointUrl",
                    connection.Endpoint.Url),
                 new KeyValuePair<string, object?>("securityMode",
                    connection.Endpoint.SecurityMode)
            };

            if (connection.Group != null &&
                !tags.ContainsKey(Constants.ConnectionGroupTag))
            {
                TagList.Add(Constants.ConnectionGroupTag, connection.Group);
            }
        }
    }
}
