// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Identity {

    /// <summary>
    /// Configuration for service
    /// </summary>
    public interface IUserManagerConfig {

        /// <summary>
        /// Identitymanager service url
        /// </summary>
        string IdentityServiceUrl { get; }
    }
}
