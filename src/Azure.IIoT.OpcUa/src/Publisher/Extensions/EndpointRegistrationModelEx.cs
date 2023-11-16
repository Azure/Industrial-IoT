// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

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
        public static bool IsSameAs(this IReadOnlyList<EndpointRegistrationModel>? model,
            IReadOnlyList<EndpointRegistrationModel>? that)
        {
            if (model == that)
            {
                return true;
            }
            if (model == null || that == null)
            {
                return false;
            }
            if (model.Count != that.Count)
            {
                return false;
            }
            foreach (var a in model)
            {
                if (!that.Any(b => b.IsSameAs(a)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this EndpointRegistrationModel? model,
            EndpointRegistrationModel? that)
        {
            if (model == that)
            {
                return true;
            }
            if (model == null || that == null)
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

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static EndpointRegistrationModel? Clone(this EndpointRegistrationModel? model)
        {
            return model == null ? null : (model with
            {
                Endpoint = model.Endpoint.Clone(),
                AuthenticationMethods = model.AuthenticationMethods?
                    .Select(c => c.Clone())
                    .ToList()
            });
        }
    }
}
