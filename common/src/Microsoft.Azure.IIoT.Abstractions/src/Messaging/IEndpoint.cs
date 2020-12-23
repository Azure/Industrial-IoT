// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using Microsoft.Azure.IIoT.Auth;
    using System;

    /// <summary>
    /// Hub Endpoint lookup
    /// </summary>
    public interface IEndpoint<THub> : IIdentityTokenGenerator {

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