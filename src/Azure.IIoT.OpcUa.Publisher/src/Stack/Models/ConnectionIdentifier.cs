// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Lookup key for connections
    /// </summary>
    internal sealed class ConnectionIdentifier
    {
        /// <summary>
        /// Create new key
        /// </summary>
        /// <param name="connection"></param>
        public ConnectionIdentifier(ConnectionModel connection)
        {
            Connection = connection?.Clone() ??
                throw new ArgumentNullException(nameof(connection));
            _hash = Connection.CreateConsistentHash();
        }

        /// <summary>
        /// Create new key
        /// </summary>
        /// <param name="endpoint"></param>
        public ConnectionIdentifier(EndpointModel endpoint)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            Connection = new ConnectionModel
            {
                Endpoint = endpoint.Clone()
            };
            _hash = Connection.CreateConsistentHash();
        }

        /// <summary>
        /// The endpoint wrapped as key
        /// </summary>
        public ConnectionModel Connection { get; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is string s)
            {
                return s == ToString();
            }
            if (obj is not ConnectionIdentifier key)
            {
                return false;
            }
            if (!Connection.IsSameAs(key.Connection))
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public static bool operator ==(ConnectionIdentifier r1,
            ConnectionIdentifier r2) =>
            EqualityComparer<ConnectionIdentifier>.Default.Equals(r1, r2);
        /// <inheritdoc/>
        public static bool operator !=(ConnectionIdentifier r1,
            ConnectionIdentifier r2) =>
            !(r1 == r2);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _hash;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Connection.CreateConnectionId() ?? "Bad connection";
        }

        private readonly int _hash;
    }
}
