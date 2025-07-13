// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class EndpointRegistrationModelEx
    {
        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this EndpointRegistrationModel? model,
            EndpointRegistrationModel? that)
        {
            if (ReferenceEquals(model, that))
            {
                return true;
            }
            if (model is null || that is null)
            {
                return false;
            }
            if (!model.Endpoint.HasSameSecurityProperties(that.Endpoint))
            {
                return false;
            }
            if (!model.AuthenticationMethods.IsSameAs(that.AuthenticationMethods))
            {
                return false;
            }
            return
                model.EndpointUrl == that.EndpointUrl &&
                model.SiteId == that.SiteId &&
                model.DiscovererId == that.DiscovererId &&
                model.SecurityLevel == that.SecurityLevel;
        }
    }
}
