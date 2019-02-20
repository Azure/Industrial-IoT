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
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        /// <summary>
        /// The endpoint wrapped as key
        /// </summary>
        public EndpointModel Endpoint { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            return obj is EndpointIdentifier key &&
                key != null &&
                Endpoint.Url == key.Endpoint.Url &&
                (Endpoint.User?.Type ?? CredentialType.None) ==
                    (key.Endpoint.User?.Type ?? CredentialType.None) &&
                (Endpoint.SecurityMode ?? SecurityMode.Best) ==
                    (key.Endpoint.SecurityMode ?? SecurityMode.Best) &&
                Endpoint.SecurityPolicy == key.Endpoint.SecurityPolicy &&
                JToken.DeepEquals(Endpoint.User?.Value,
                    key.Endpoint.User?.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = -1971667340;
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(Endpoint.SecurityPolicy);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(Endpoint.Url);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<CredentialType?>.Default.GetHashCode(
                    Endpoint.User?.Type ?? CredentialType.None);
            hashCode = hashCode * -1521134295 +
               EqualityComparer<SecurityMode?>.Default.GetHashCode(
                   Endpoint.SecurityMode ?? SecurityMode.Best);
            hashCode = hashCode * -1521134295 +
                JToken.EqualityComparer.GetHashCode(Endpoint.User?.Value);
            return hashCode;
        }
    }

}
