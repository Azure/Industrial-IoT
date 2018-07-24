// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure {

    public interface ISubscriptionInfoProvider {

        /// <summary>
        /// Provides subscription information for an
        /// azure environment
        /// </summary>
        /// <returns></returns>
        ISubscriptionInfo GetSubscriptionInfo();
    }
}
