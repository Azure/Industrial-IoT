// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Application validation services
    /// </summary>
    public interface IOpcUaDiscoveryServices {

        /// <summary>
        /// Read entire application model from discovery
        /// server using discovery url
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <returns></returns>
        Task<List<ApplicationRegistrationModel>> DiscoverApplicationsAsync(
            Uri discoveryUrl);
    }
}
