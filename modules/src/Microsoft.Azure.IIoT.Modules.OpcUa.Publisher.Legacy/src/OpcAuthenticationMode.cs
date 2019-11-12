// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher
{
    /// <summary>
    /// Enum that defines the authentication method to connect to OPC UA
    /// </summary>
    public enum OpcAuthenticationMode
    {
        /// <summary>
        /// Anonymous authentication
        /// </summary>
        Anonymous,
        /// <summary>
        /// Username/Password authentication
        /// </summary>
        UsernamePassword
    }
}
