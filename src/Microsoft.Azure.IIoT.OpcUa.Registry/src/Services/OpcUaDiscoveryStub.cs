// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services{
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery stub
    /// </summary>
    public class OpcUaDiscoveryStub : IOpcUaDiscoveryServices {

        /// <inheritdoc/>
        public Task<DiscoveryResultModel> DiscoverApplicationsAsync(
            Uri discoveryUrl) {
            throw new ResourceNotFoundException("Application discovery not supported");
        }
    }
}
