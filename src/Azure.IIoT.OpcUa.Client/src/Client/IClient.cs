// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    /// <summary>
    /// Client capabilities
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// Manage certificates throuhg the certificates
        /// management api
        /// </summary>
        ICertificates Certificates { get; }
    }
}
