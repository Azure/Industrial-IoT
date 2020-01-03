// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Edge gateway registration
    /// </summary>
    [Serializable]
    public sealed class GatewayRegistration : DeviceRegistration {

        /// <inheritdoc/>
        public override string DeviceType => "Gateway";

        /// <inheritdoc/>
        public override string Thumbprint => null;

        /// <inheritdoc/>
        public override Dictionary<string, string> Certificate => null;

        /// <summary>
        /// Create registration - for testing purposes
        /// </summary>
        /// <param name="deviceId"></param>
        public GatewayRegistration(string deviceId = null) {
            DeviceId = deviceId;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            var registration = obj as GatewayRegistration;
            return base.Equals(registration);
        }

        /// <inheritdoc/>
        public static bool operator ==(GatewayRegistration r1,
            GatewayRegistration r2) => EqualityComparer<GatewayRegistration>.Default.Equals(r1, r2);

        /// <inheritdoc/>
        public static bool operator !=(GatewayRegistration r1,
            GatewayRegistration r2) => !(r1 == r2);

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            return hashCode;
        }

        internal bool IsInSync() {
            return _isInSync;
        }

        internal bool IsConnected() {
            return Connected;
        }

        internal bool _isInSync;
    }
}
