// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

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
            _hash = CreateHash(Endpoint);
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
            if (Endpoint.Url != key.Endpoint.Url) {
                return false;
            }
            if ((Endpoint.User?.Type ?? CredentialType.None) !=
                    (key.Endpoint.User?.Type ?? CredentialType.None)) {
                return false;
            }
            if ((Endpoint.SecurityMode ?? SecurityMode.Best) !=
                    (key.Endpoint.SecurityMode ?? SecurityMode.Best)) {
                return false;
            }
            if (Endpoint.SecurityPolicy != key.Endpoint.SecurityPolicy) {
                return false;
            }
            if (!JToken.DeepEquals(Endpoint.User?.Value,
                    key.Endpoint.User?.Value)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return _hash;
        }

        /// <summary>
        /// Create unique hash
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static int CreateHash(EndpointModel endpoint) {
            var hashCode = -1971667340;
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(endpoint.SecurityPolicy);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(endpoint.Url);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<CredentialType?>.Default.GetHashCode(
                   endpoint.User?.Type ?? CredentialType.None);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<SecurityMode?>.Default.GetHashCode(
                    endpoint.SecurityMode ?? SecurityMode.Best);
            hashCode = (hashCode * -1521134295) +
                JToken.EqualityComparer.GetHashCode(endpoint.User?.Value);
            return hashCode;
        }

        private readonly int _hash;
    }
}
