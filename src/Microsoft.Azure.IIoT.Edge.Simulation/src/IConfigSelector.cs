// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Management {
    using System.Collections.Generic;

    public interface IConfigSelector {

        /// <summary>
        /// Select subscription to use
        /// </summary>
        /// <param name="subscriptions"></param>
        /// <returns></returns>
        string SelectSubscription(IEnumerable<string> subscriptions);

        /// <summary>
        /// Select region
        /// </summary>
        /// <returns></returns>
        string SelectRegion(IEnumerable<string> regions);
    }
}