// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.Registry.Models
{
    using Furly.Extensions.Serializers;
    using Furly.Azure.IoT;
    using Furly.Azure.IoT.Models;
    using System;

    /// <summary>
    /// Entity registration extensions
    /// </summary>
    public static class EntityRegistrationEx
    {
        /// <summary>
        /// Convert twin to registration information.
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public static EntityRegistration ToEntityRegistration(this DeviceTwinModel twin,
            bool onlyServerState = false)
        {
            if (twin == null)
            {
                return null;
            }
            var type = twin.Tags.GetValueOrDefault<string>(nameof(EntityRegistration.DeviceType), null);
            if (string.IsNullOrEmpty(type) && twin.Reported != null)
            {
                type = twin.Reported.GetValueOrDefault<string>(TwinProperty.Type, null);
            }
            if (string.IsNullOrEmpty(type))
            {
                type = twin.Tags.GetValueOrDefault<string>(TwinProperty.Type, null);
            }
            if (IdentityType.Gateway.EqualsIgnoreCase(type))
            {
                return twin.ToGatewayRegistration();
            }
            if (IdentityType.Application.EqualsIgnoreCase(type))
            {
                return twin.ToApplicationRegistration();
            }
            if (IdentityType.Endpoint.EqualsIgnoreCase(type))
            {
                return twin.ToEndpointRegistration(onlyServerState);
            }
            if (IdentityType.Publisher.EqualsIgnoreCase(type))
            {
                return twin.ToPublisherRegistration(onlyServerState);
            }
            // ...
            return null;
        }
    }
}
