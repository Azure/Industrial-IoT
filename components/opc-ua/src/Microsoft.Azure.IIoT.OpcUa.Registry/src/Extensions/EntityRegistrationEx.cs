// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Newtonsoft.Json.Linq;

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
                if (string.IsNullOrEmpty(type)) {
                    type = twin.Tags.GetValueOrDefault<string>(TwinProperty.Type, null);
                }
            }
            switch (type?.ToLowerInvariant() ?? "") {
                case IdentityType.Gateway:
                    return twin.ToGatewayRegistration();
                case IdentityType.Application:
                    return twin.ToApplicationRegistration();
                case IdentityType.Endpoint:
                    return twin.ToEndpointRegistration(onlyServerState);
                case IdentityType.Supervisor:
                    return twin.ToSupervisorRegistration(onlyServerState);
                case IdentityType.Publisher:
                    return twin.ToPublisherRegistration(onlyServerState);
                case IdentityType.Discoverer:
                    return twin.ToDiscovererRegistration(onlyServerState);
                // ...
            }
            return null;
        }
    }
}
