// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Registry.Models
{
    using Furly.Azure.IoT.Models;
    using Furly.Extensions.Serializers;
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
                type = twin.Reported.GetValueOrDefault(OpcUa.Constants.TwinPropertyTypeKey, (string)null);
            }
            if (string.IsNullOrEmpty(type))
            {
                type = twin.Tags.GetValueOrDefault(OpcUa.Constants.TwinPropertyTypeKey, (string)null);
            }
            if (Constants.EntityTypeGateway.EqualsIgnoreCase(type))
            {
                return twin.ToGatewayRegistration();
            }
            if (Constants.EntityTypeApplication.EqualsIgnoreCase(type))
            {
                return twin.ToApplicationRegistration();
            }
            if (Constants.EntityTypeEndpoint.EqualsIgnoreCase(type))
            {
                return twin.ToEndpointRegistration(onlyServerState);
            }
            if (Constants.EntityTypePublisher.EqualsIgnoreCase(type))
            {
                return twin.ToPublisherRegistration(onlyServerState);
            }
            // ...
            return null;
        }
    }
}
