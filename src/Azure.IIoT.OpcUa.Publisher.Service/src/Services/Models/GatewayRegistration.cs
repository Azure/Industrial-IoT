// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Services.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Edge gateway registration
    /// </summary>
    [DataContract]
    public sealed class GatewayRegistration : EntityRegistration
    {
        /// <inheritdoc/>
        [DataMember]
        public override string DeviceType => Constants.EntityTypeGateway;

        /// <summary>
        /// Create registration - for testing purposes
        /// </summary>
        /// <param name="deviceId"></param>
        public GatewayRegistration(string? deviceId = null)
        {
            DeviceId = deviceId;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
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
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal bool IsConnected()
        {
            return Connected;
        }
    }
}
