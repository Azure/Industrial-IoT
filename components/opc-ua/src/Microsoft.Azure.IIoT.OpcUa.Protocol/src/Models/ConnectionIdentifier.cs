// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System;

    /// <summary>
    /// Lookup key for connections
    /// </summary>
    public sealed class ConnectionIdentifier {

        /// <summary>
        /// Create new key
        /// </summary>
        /// <param name="connection"></param>
        public ConnectionIdentifier(ConnectionModel connection) {
            Connection = connection?.Clone() ??
                throw new ArgumentNullException(nameof(connection));
            _hash = Connection.CreateConsistentHash();
        }

        /// <summary>
        /// Create new key
        /// </summary>
        /// <param name="endpoint"></param>
        public ConnectionIdentifier(EndpointModel endpoint) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            Connection = new ConnectionModel {
                Endpoint = endpoint.Clone()
            };
            _hash = Connection.CreateConsistentHash();
        }

        /// <summary>
        /// The endpoint wrapped as key
        /// </summary>
        public ConnectionModel Connection { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (obj is string s) {
                return s == ToString();
            }
            if (!(obj is ConnectionIdentifier key)) {
                return false;
            }
            if (!Connection.IsSameAs(key.Connection)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return _hash;
        }

        /// <inheritdoc/>
        public override string ToString() {
            return Connection.CreateConnectionId();
        }

        private readonly int _hash;
    }

}
