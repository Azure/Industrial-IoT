// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure {
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using System.Threading.Tasks;

    public static class SubscriptionInfoEx {

        /// <summary>
        /// Environment of subscription
        /// </summary>
        public static async Task<AzureEnvironment> GetAzureEnvironmentAsync(
            this ISubscriptionInfo info) => AzureEnvironmentEx.FromName(
                await info.GetEnvironment());
    }
}
