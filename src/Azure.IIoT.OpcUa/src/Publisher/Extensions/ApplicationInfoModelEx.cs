// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class ApplicationInfoModelEx
    {
        /// <summary>
        /// Create unique application id
        /// </summary>
        /// <param name="siteOrGatewayId"></param>
        /// <param name="applicationUri"></param>
        /// <param name="applicationType"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(applicationUri))]
        public static string? CreateApplicationId(string? siteOrGatewayId,
            string? applicationUri, ApplicationType? applicationType)
        {
            if (string.IsNullOrEmpty(applicationUri))
            {
                return null;
            }
#pragma warning disable CA1308 // Normalize strings to uppercase
            applicationUri = applicationUri.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
            var type = applicationType ?? ApplicationType.Server;
            var id = $"{siteOrGatewayId ?? ""}-{type}-{applicationUri}";
            var prefix = applicationType == ApplicationType.Client ? "uac" : "uas";
            return prefix + id.ToSha1Hash();
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this ApplicationInfoModel? model,
            ApplicationInfoModel? that)
        {
            if (ReferenceEquals(model, that))
            {
                return true;
            }
            if (model is null || that is null)
            {
                return false;
            }
            return
                StringComparer.OrdinalIgnoreCase.Equals(that.ApplicationUri, model.ApplicationUri) &&
                that.ApplicationType == model.ApplicationType;
        }
    }
}
