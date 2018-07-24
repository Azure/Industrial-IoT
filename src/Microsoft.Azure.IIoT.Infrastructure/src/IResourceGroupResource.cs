// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure {
    using System;

    public interface IResourceGroupResource : IResource, IDisposable {

        /// <summary>
        /// Subscription of resource group
        /// </summary>
        ISubscriptionInfo Subscription { get; }
    }
}
