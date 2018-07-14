// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Management {

    public interface IManagementConfig {

        /// <summary>
        /// Select subscription to use
        /// </summary>
        string SubscriptionId { get; }

        /// <summary>
        /// Selected region
        /// </summary>
        string Region { get; }
    }
}