// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services {
    using System;

    /// <summary>
    /// Endpoint lookup
    /// </summary>
    public interface IEndpoint {

        /// <summary>
        /// Resource name
        /// </summary>
        string Resource { get; }

        /// <summary>
        /// Get client endpoint
        /// </summary>
        /// <returns>Client endpoint</returns>
        Uri EndpointUrl { get; }
    }
}