// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class EndpointInfoModelEx
    {
        /// <summary>
        /// Create unique endpoint
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="url"></param>
        /// <param name="mode"></param>
        /// <param name="securityPolicy"></param>
        /// <returns></returns>
        public static string? CreateEndpointId(string? applicationId, string? url,
            SecurityMode? mode, string? securityPolicy)
        {
            if (applicationId == null || url == null)
            {
                return null;
            }

#pragma warning disable CA1308 // Normalize strings to uppercase
            url = url.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
            mode ??= SecurityMode.SignAndEncrypt;
#pragma warning disable CA1308 // Normalize strings to uppercase
            securityPolicy = securityPolicy?.ToLowerInvariant() ?? "";
#pragma warning restore CA1308 // Normalize strings to uppercase

            var id = $"{url}-{applicationId}-{mode}-{securityPolicy}";
            return "uat" + id.ToSha1Hash();
        }

        /// <summary>
        /// Checks whether the identifier is an endpoint id
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public static bool IsEndpointId(string? endpointId)
        {
            if (string.IsNullOrWhiteSpace(endpointId))
            {
                return false;
            }
            if (!endpointId.StartsWith("uat", StringComparison.Ordinal))
            {
                return false;
            }
            var str = endpointId.Substring(3);
            if (str.Length % 2 != 0)
            {
                return false;
            }
            for (var i = 0; i < str.Length; i += 2)
            {
                var s = str.Substring(i, 2);
                if (!byte.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out _))
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
        public static bool IsSameAs(this IReadOnlyList<EndpointInfoModel> model,
            IReadOnlyList<EndpointInfoModel> that)
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
            return model.All(a => that.Any(b => b.IsSameAs(a)));
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this EndpointInfoModel model,
            EndpointInfoModel that)
        {
            if (model == that)
            {
                return true;
            }
            if (model == null || that == null)
            {
                return false;
            }
            return
                that.Registration.IsSameAs(model.Registration);
        }
    }
}
