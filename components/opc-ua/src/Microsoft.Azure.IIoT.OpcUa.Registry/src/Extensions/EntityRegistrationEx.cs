// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;

    /// <summary>
    /// Entity registration extensions
    /// </summary>
    public static class EntityRegistrationEx {

        /// <summary>
        /// Convert twin to registration information.
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public static EntityRegistration ToEntityRegistration(this DeviceTwinModel twin,
            bool onlyServerState = false) {
            if (twin == null) {
                return null;
            }
            var type = twin.Tags.GetValueOrDefault<string>(nameof(EntityRegistration.DeviceType), null);
            if (string.IsNullOrEmpty(type) && twin.Properties.Reported != null) {
                type = twin.Properties.Reported.GetValueOrDefault<string>(TwinProperty.Type, null);
            }
            if (string.IsNullOrEmpty(type)) {
                type = twin.Tags.GetValueOrDefault<string>(TwinProperty.Type, null);
            }
            if (IdentityType.Gateway.EqualsIgnoreCase(type)) {
                return twin.ToGatewayRegistration();
            }
            if (IdentityType.Application.EqualsIgnoreCase(type)) {
                return twin.ToApplicationRegistration();
            }
            if (IdentityType.Endpoint.EqualsIgnoreCase(type)) {
                return twin.ToEndpointRegistration(onlyServerState);
            }
            if (IdentityType.Supervisor.EqualsIgnoreCase(type)) {
                return twin.ToSupervisorRegistration(onlyServerState);
            }
            if (IdentityType.Publisher.EqualsIgnoreCase(type)) {
                return twin.ToPublisherRegistration(onlyServerState);
            }
            if (IdentityType.Discoverer.EqualsIgnoreCase(type)) {
                return twin.ToDiscovererRegistration(onlyServerState);
            }
            // ...
            return null;
        }
    }
}
