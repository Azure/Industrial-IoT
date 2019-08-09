// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System;

    /// <summary>
    /// Lookup key for endpoint clients
    /// </summary>
    public sealed class EndpointIdentifier {

        /// <summary>
        /// Create new key
        /// </summary>
        /// <param name="endpoint"></param>
        public EndpointIdentifier(EndpointModel endpoint) {
            Endpoint = endpoint?.Clone() ??
                throw new ArgumentNullException(nameof(endpoint));
            _hash = Endpoint.CreateConsistentHash();
        }

        /// <summary>
        /// The endpoint wrapped as key
        /// </summary>
        public EndpointModel Endpoint { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is EndpointIdentifier key)) {
                return false;
            }
            if (!Endpoint.IsSameAs(key.Endpoint)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return _hash;
        }

        private readonly int _hash;
    }
}
